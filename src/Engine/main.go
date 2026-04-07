package main

import (
	"log"
	"os"
	"os/signal"
	"syscall"
	"helion-engine/database"
	"helion-engine/router"
)

func main() {
	if (os.Getenv("HELION_INTERNAL_PORT") == "" && os.Getenv("HELION_UDS_PATH") == "") || os.Getenv("HELION_IPC_TOKEN") == "" {
		log.Fatal("Fatal: Missing C# IPC Handshake environment variables.")
	}

	systemDbUri := os.Getenv("HELION_SYSTEM_DB")
	
	// 20-Pass Fix: Initialize Pooler with LRU Cache for SQLite
	poolManager := database.NewConnectionManager(systemDbUri)
	defer poolManager.CloseAll()

	// Start UDS or TCP Server
	go router.Serve(poolManager)

	stop := make(chan os.Signal, 1)
	signal.Notify(stop, os.Interrupt, syscall.SIGTERM)
	<-stop

	log.Println("Helion Engine shutting down gracefully...")
}
