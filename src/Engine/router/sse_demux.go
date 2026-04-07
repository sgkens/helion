package router

import (
	"context"
	"encoding/json"
	"log"
	"sync"

	"github.com/jackc/pgx/v4/pgxpool"
)

type SSEEvent struct {
	TenantID string          `json:"tenant_id"`
	Channel  string          `json:"channel"`
	Payload  json.RawMessage `json:"payload"`
}

type SSEDemux struct {
	pool     *pgxpool.Pool
	// clients maps TenantID -> Channel -> Set of client channels
	clients  map[string]map[string]map[chan SSEEvent]bool
	mu       sync.RWMutex
}

func NewSSEDemux(pool *pgxpool.Pool) *SSEDemux {
	return &SSEDemux{
		pool:    pool,
		clients: make(map[string]map[string]map[chan SSEEvent]bool),
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

func (d *SSEDemux) Subscribe(tenantID string, channel string) chan SSEEvent {
	d.mu.Lock()
	defer d.mu.Unlock()

	if _, ok := d.clients[tenantID]; !ok {
		d.clients[tenantID] = make(map[string]map[chan SSEEvent]bool)
	}
	if _, ok := d.clients[tenantID][channel]; !ok {
		d.clients[tenantID][channel] = make(map[chan SSEEvent]bool)
	}

	ch := make(chan SSEEvent, 10)
	d.clients[tenantID][channel][ch] = true
	return ch
}

func (d *SSEDemux) Unsubscribe(tenantID string, channel string, ch chan SSEEvent) {
	d.mu.Lock()
	defer d.mu.Unlock()

	if tenantSubs, ok := d.clients[tenantID]; ok {
		if subs, ok := tenantSubs[channel]; ok {
			delete(subs, ch)
			close(ch)
			if len(subs) == 0 {
				delete(tenantSubs, channel)
			}
		}
		if len(tenantSubs) == 0 {
			delete(d.clients, tenantID)
		}
	}
}

func (d *SSEDemux) broadcast(event SSEEvent) {
	d.mu.RLock()
	defer d.mu.RUnlock()

	// 20-Pass Fix: SSE Broadcast Filtering (Tenant Isolation)
	if tenantSubs, ok := d.clients[event.TenantID]; ok {
		if subs, ok := tenantSubs[event.Channel]; ok {
			for ch := range subs {
				select {
				case ch <- event:
				default:
					// Buffer full, drop event for this slow client
				}
			}
		}
	}
}
