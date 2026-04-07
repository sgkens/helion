package router

import (
	"bytes"
	"encoding/json"
	"fmt"
	"net/http"
	"runtime/debug"
)

func PanicRecoveryMiddleware(next http.HandlerFunc) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		defer func() {
			if err := recover(); err != nil {
				stack := debug.Stack()
				panicMsg := fmt.Sprintf("Go Engine Panic: %v\n%s", err, stack)
				
				w.Header().Set("Content-Type", "application/json")
				w.WriteHeader(http.StatusInternalServerError)
				json.NewEncoder(w).Encode(map[string]string{
					"error": "Data Engine Failure",
					"details": "The query execution caused an engine panic.",
				})
				
				reportPanicToGateway(panicMsg)
			}
		}()
		next.ServeHTTP(w, r)
	}
}

func reportPanicToGateway(panicStr string) {
	// Posts the raw stack trace back to the C# LogDispatcher's internal loopback port
	// Note: Port 49151 is used as an example in PDR, we should probably make this configurable
	payload := map[string]string{"message": panicStr}
	body, _ := json.Marshal(payload)
	http.Post("http://127.0.0.1:49151/internal/telemetry/report", "application/json", bytes.NewBuffer(body))
}
