package router

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"net"
	"net/http"
	"os"

	"helion-engine/api"
	"helion-engine/database"
	"helion-engine/telemetry"
)

type EngineRouter struct {
	poolManager *database.ConnectionManager
	ipcToken    string
	pgMonitor   *telemetry.PgMonitor
	sseDemux    *SSEDemux
}

func Serve(pm *database.ConnectionManager) {
	port := os.Getenv("HELION_INTERNAL_PORT")
	udsPath := os.Getenv("HELION_UDS_PATH")
	token := os.Getenv("HELION_IPC_TOKEN")

	mux := http.NewServeMux()
	router := &EngineRouter{
		poolManager: pm,
		ipcToken:    token,
	}

	if systemPool := pm.GetSystemPool(); systemPool != nil {
		router.pgMonitor = &telemetry.PgMonitor{Pool: systemPool}
		router.sseDemux = NewSSEDemux(systemPool)
		go router.sseDemux.Start(context.Background())
	}

	// 1. REST API with Streaming Support
	mux.HandleFunc("/api/v1/rest/", router.requireIpc(PanicRecoveryMiddleware(router.handleRestRequest)))

	// 2. Telemetry API
	mux.HandleFunc("/api/v1/telemetry/slow-queries", router.requireIpc(PanicRecoveryMiddleware(router.handleSlowQueries)))

	// 3. SSE Real-time Subscriptions
	mux.HandleFunc("/api/v1/stream", router.requireIpc(PanicRecoveryMiddleware(router.handleSSERequest)))

	// 4. DDL Schema Management
	mux.HandleFunc("/api/v1/schema", router.requireIpc(PanicRecoveryMiddleware(router.handleSchemaRequest)))

	// 5. Health Check
	mux.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusOK)
		w.Write([]byte("ready"))
	})

	server := &http.Server{
		Handler: mux,
	}

	if udsPath != "" {
		// 20-Pass Fix: Unix Domain Sockets (UDS)
		if err := os.RemoveAll(udsPath); err != nil {
			log.Fatal(err)
		}

		listener, err := net.Listen("unix", udsPath)
		if err != nil {
			log.Fatal(err)
		}
		log.Printf("Engine listening on UDS: %s", udsPath)
		if err := server.Serve(listener); err != nil {
			log.Fatal(err)
		}
	} else {
		addr := fmt.Sprintf("127.0.0.1:%s", port)
		log.Printf("Engine listening on TCP: %s", addr)
		server.Addr = addr
		if err := server.ListenAndServe(); err != nil {
			log.Fatal(err)
		}
	}
}

func (er *EngineRouter) requireIpc(next http.HandlerFunc) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		token := r.Header.Get("X-Helion-Ipc-Token")
		if token == "" || token != er.ipcToken {
			http.Error(w, "Unauthorized IPC Request", http.StatusForbidden)
			return
		}
		next.ServeHTTP(w, r)
	}
}

func (er *EngineRouter) handleRestRequest(w http.ResponseWriter, r *http.Request) {
	// 20-Pass Fix: Tenant Resolution (Header check)
	tenantID := r.Header.Get("X-Helion-Tenant-Id")
	if tenantID == "" {
		http.Error(w, "Missing X-Helion-Tenant-Id header", http.StatusBadRequest)
		return
	}

	// For the prototype, we assume Postgres for simplicity if not otherwise specified
	pool := er.poolManager.GetSystemPool() 
	if pool == nil {
		http.Error(w, "System pool unavailable", http.StatusInternalServerError)
		return
	}

	handler := &api.StreamHandler{Pool: pool}
	handler.ServeHTTP(w, r)
}

func (er *EngineRouter) handleSchemaRequest(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var payload api.DDLPayload
	if err := json.NewDecoder(r.Body).Decode(&payload); err != nil {
		http.Error(w, "Invalid JSON payload", http.StatusBadRequest)
		return
	}

	tenantID := r.Header.Get("X-Helion-Tenant-Id")
	if tenantID == "" {
		http.Error(w, "Missing X-Helion-Tenant-Id header", http.StatusBadRequest)
		return
	}

	pool := er.poolManager.GetSystemPool() // Placeholder
	if pool == nil {
		http.Error(w, "System pool unavailable", http.StatusInternalServerError)
		return
	}

	if err := api.HandleDDLRequest(r.Context(), pool, payload); err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.WriteHeader(http.StatusOK)
	w.Write([]byte(`{"status": "success"}`))
}

func (er *EngineRouter) handleSSERequest(w http.ResponseWriter, r *http.Request) {
	if er.sseDemux == nil {
		http.Error(w, "SSE demux unavailable", http.StatusServiceUnavailable)
		return
	}

	channel := r.URL.Query().Get("channel")
	if channel == "" {
		http.Error(w, "Missing channel parameter", http.StatusBadRequest)
		return
	}

	flusher, ok := w.(http.Flusher)
	if !ok {
		http.Error(w, "Streaming not supported", http.StatusInternalServerError)
		return
	}

	ch := er.sseDemux.Subscribe(channel)
	defer er.sseDemux.Unsubscribe(channel, ch)

	w.Header().Set("Content-Type", "text/event-stream")
	w.Header().Set("Cache-Control", "no-cache")
	w.Header().Set("Connection", "keep-alive")

	for {
		select {
		case event := <-ch:
			fmt.Fprintf(w, "data: %s\n\n", event.Payload)
			flusher.Flush()
		case <-r.Context().Done():
			return
		}
	}
}

func (er *EngineRouter) handleSlowQueries(w http.ResponseWriter, r *http.Request) {
	if er.pgMonitor == nil {
		http.Error(w, "Telemetry monitor unavailable (System DB connection failed)", http.StatusServiceUnavailable)
		return
	}

	stats, err := er.pgMonitor.GetTopSlowQueries(r.Context(), 10)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}

	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(stats)
}
