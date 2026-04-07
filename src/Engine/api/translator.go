package api

import (
	"fmt"
	"net/url"
	"strings"
)

type QueryDefinition struct {
	BaseSQL    string
	Where      string
	Parameters []interface{}
}

// TranslateUrlQuery translates URL query parameters to SQL
func TranslateUrlQuery(tableName string, params url.Values) (*QueryDefinition, error) {
	if err := SanitizeIdentifier(tableName); err != nil {
		return nil, err
	}

	baseSQL := fmt.Sprintf("SELECT * FROM %s", tableName)
	whereClauses := []string{}
	parameters := []interface{}{}

	for key, values := range params {
		if key == "table" {
			continue // Already handled
		}

		if err := SanitizeIdentifier(key); err != nil {
			continue // Skip invalid identifiers
		}

		// Simple equality for now: ?column=value
		whereClauses = append(whereClauses, fmt.Sprintf("%s = $%d", key, len(parameters)+1))
		parameters = append(parameters, values[0])
	}

	where := ""
	if len(whereClauses) > 0 {
		where = " WHERE " + strings.Join(whereClauses, " AND ")
	}

	return &QueryDefinition{
		BaseSQL:    baseSQL,
		Where:      where,
		Parameters: parameters,
	}, nil
}
