package api

import (
	"context"
	"encoding/json"
	"net/http"
	"time"
	"github.com/jackc/pgx/v4/pgxpool"
)

type StreamHandler struct {
	Pool *pgxpool.Pool
}

func (h *StreamHandler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	// Pass 4 Fix: Enforce a strict 15-second execution timeout on the database layer
	ctx, cancel := context.WithTimeout(r.Context(), 15 * time.Second)
	defer cancel()

	// 20-Pass Fix: Context-aware query translation
	tableName := r.URL.Query().Get("table")
	if tableName == "" {
		http.Error(w, "table parameter is required", http.StatusBadRequest)
		return
	}

	queryDef, err := TranslateUrlQuery(tableName, r.URL.Query()) 
	if err != nil {
		http.Error(w, err.Error(), http.StatusBadRequest)
		return
	}

	rows, err := h.Pool.Query(ctx, queryDef.BaseSQL + queryDef.Where, queryDef.Parameters...)
	if err != nil {
		http.Error(w, err.Error(), http.StatusInternalServerError)
		return
	}
	defer rows.Close()

	w.Header().Set("Content-Type", "application/json")
	flusher, ok := w.(http.Flusher)
	if !ok {
		// Fallback for non-flushing clients or just report error
		http.Error(w, "Streaming not supported", http.StatusInternalServerError)
		return
	}

	w.Write([]byte("["))
	first := true
	encoder := json.NewEncoder(w)

	for rows.Next() {
		if !first {
			w.Write([]byte(","))
		}
		first = false

		values, err := rows.Values()
		if err != nil {
			// In a middle of a stream, error handling is tricky.
			// Usually we close the connection or write an error object if we can.
			break
		}
		encoder.Encode(values)
		flusher.Flush()
	}

	w.Write([]byte("]"))
	flusher.Flush()
}
