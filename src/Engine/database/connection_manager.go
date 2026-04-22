package database

import (
	"context"
	"database/sql"
	"fmt"
	"log"
	"sync"

	lru "github.com/hashicorp/golang-lru/v2"
	"github.com/jackc/pgx/v4/pgxpool"
	_ "github.com/mattn/go-sqlite3"
)

type ConnectionManager struct {
	systemDbUri string
	systemPool  *pgxpool.Pool
	sqliteCache *lru.Cache[string, *sql.DB]
	pgPools     map[string]*pgxpool.Pool
	mu          sync.RWMutex
}

func NewConnectionManager(systemDbUri string) *ConnectionManager {
	// 20-Pass Fix: Initialize Pooler with LRU Cache for SQLite (500 tenants)
	cache, _ := lru.NewWithEvict[string, *sql.DB](500, func(key string, db *sql.DB) {
		log.Printf("Evicting SQLite connection for tenant: %s", key)
		db.Close()
	})

	cm := &ConnectionManager{
		systemDbUri: systemDbUri,
		sqliteCache: cache,
		pgPools:     make(map[string]*pgxpool.Pool),
	}

	if systemDbUri != "" {
		pool, err := pgxpool.Connect(context.Background(), systemDbUri)
		if err != nil {
			log.Printf("Warning: Could not connect to system DB: %v", err)
		} else {
			cm.systemPool = pool
		}
	}

	return cm
}

func (cm *ConnectionManager) GetSystemPool() *pgxpool.Pool {
	return cm.systemPool
}

func (cm *ConnectionManager) GetTenantPool(tenantID string, isPostgres bool, connectionString string) (interface{}, error) {
	if isPostgres {
		cm.mu.RLock()
		pool, ok := cm.pgPools[tenantID]
		cm.mu.RUnlock()

		if ok {
			return pool, nil
		}

		cm.mu.Lock()
		defer cm.mu.Unlock()

		// Double-check
		if pool, ok := cm.pgPools[tenantID]; ok {
			return pool, nil
		}

		pool, err := pgxpool.Connect(context.Background(), connectionString)
		if err != nil {
			return nil, err
		}
		cm.pgPools[tenantID] = pool
		return pool, nil
	} else {
		// SQLite
		if db, ok := cm.sqliteCache.Get(tenantID); ok {
			return db, nil
		}

		// connectionString is the path to the .db file
		// 20-Pass Fix: Enable WAL mode for SQLite
		db, err := sql.Open("sqlite3", fmt.Sprintf("%s?_journal_mode=WAL&_sync=NORMAL", connectionString))
		if err != nil {
			return nil, err
		}

		cm.sqliteCache.Add(tenantID, db)
		return db, nil
	}
}

func (cm *ConnectionManager) CloseAll() {
	log.Println("Closing all connection pools...")
	if cm.systemPool != nil {
		cm.systemPool.Close()
	}

	cm.mu.Lock()
	for _, pool := range cm.pgPools {
		pool.Close()
	}
	cm.mu.Unlock()

	cm.sqliteCache.Purge() // This will trigger eviction and close all SQLite DBs
}
