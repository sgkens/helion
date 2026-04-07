package api

import (
	"context"
	"fmt"
	"regexp"

	"github.com/jackc/pgx/v4/pgxpool"
)

var identifierRegex = regexp.MustCompile(`^[a-zA-Z0-9_]+$`)

func SanitizeIdentifier(name string) error {
	if !identifierRegex.MatchString(name) {
		return fmt.Errorf("invalid identifier: %s. Must contain only alphanumeric characters and underscores", name)
	}
	return nil
}

// DDLPayload represents a DDL request
type DDLPayload struct {
	Action    string `json:"action"`
	TableName string `json:"table_name"`
	Column    string `json:"column_name,omitempty"`
	Type      string `json:"type,omitempty"`
}

func HandleDDLRequest(ctx context.Context, pool *pgxpool.Pool, payload DDLPayload) error {
	// 20-Pass Fix: DDL Sanitization (Regex validation)
	if err := SanitizeIdentifier(payload.TableName); err != nil {
		return err
	}

	var sql string
	switch payload.Action {
	case "create_table":
		// Simplified for now: just creates a table with an ID
		sql = fmt.Sprintf("CREATE TABLE IF NOT EXISTS %s (id SERIAL PRIMARY KEY)", payload.TableName)
	case "add_column":
		if err := SanitizeIdentifier(payload.Column); err != nil {
			return err
		}
		// Strict type validation should be here too
		sql = fmt.Sprintf("ALTER TABLE %s ADD COLUMN IF NOT EXISTS %s %s", payload.TableName, payload.Column, payload.Type)
	case "drop_table":
		sql = fmt.Sprintf("DROP TABLE IF EXISTS %s", payload.TableName)
	default:
		return fmt.Errorf("unsupported DDL action: %s", payload.Action)
	}

	_, err := pool.Exec(ctx, sql)
	return err
}
