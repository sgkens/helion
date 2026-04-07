package router

import (
	"context"
	"encoding/json"
	"log"
	"sync"

	"github.com/jackc/pgx/v4/pgxpool"
)

type SSEEvent struct {
	Channel string          `json:"channel"`
	Payload json.RawMessage `json:"payload"`
}

type SSEDemux struct {
	pool     *pgxpool.Pool
	clients  map[string]map[chan SSEEvent]bool
	mu       sync.RWMutex
}

func NewSSEDemux(pool *pgxpool.Pool) *SSEDemux {
	return &SSEDemux{
		pool:    pool,
		clients: make(map[string]map[chan SSEEvent]bool),
	}
}

func (d *SSEDemux) Start(ctx context.Context) {
	// 20-Pass Fix: SSE Threading (Single goroutine for LISTEN)
	conn, err := d.pool.Acquire(ctx)
	if err != nil {
		log.Printf("SSE Demux: Failed to acquire connection: %v", err)
		return
	}
	defer conn.Release()

	_, err = conn.Exec(ctx, "LISTEN helion_events")
	if err != nil {
		log.Printf("SSE Demux: Failed to LISTEN: %v", err)
		return
	}

	for {
		notification, err := conn.Conn().WaitForNotification(ctx)
		if err != nil {
			log.Printf("SSE Demux: Error waiting for notification: %v", err)
			return
		}

		var event SSEEvent
		if err := json.Unmarshal([]byte(notification.Payload), &event); err != nil {
			// Fallback: If not JSON, use the raw payload
			event = SSEEvent{
				Channel: notification.Channel,
				Payload: json.RawMessage(notification.Payload),
			}
		}

		d.broadcast(event)
	}
}

func (d *SSEDemux) Subscribe(channel string) chan SSEEvent {
	d.mu.Lock()
	defer d.mu.Unlock()

	if _, ok := d.clients[channel]; !ok {
		d.clients[channel] = make(map[chan SSEEvent]bool)
	}

	ch := make(chan SSEEvent, 10)
	d.clients[channel][ch] = true
	return ch
}

func (d *SSEDemux) Unsubscribe(channel string, ch chan SSEEvent) {
	d.mu.Lock()
	defer d.mu.Unlock()

	if subs, ok := d.clients[channel]; ok {
		delete(subs, ch)
		close(ch)
		if len(subs) == 0 {
			delete(d.clients, channel)
		}
	}
}

func (d *SSEDemux) broadcast(event SSEEvent) {
	d.mu.RLock()
	defer d.mu.RUnlock()

	if subs, ok := d.clients[event.Channel]; ok {
		for ch := range subs {
			select {
			case ch <- event:
			default:
				// Buffer full, drop event for this slow client
			}
		}
	}
}
