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

// TranslateUrlQuery translates URL query parameters to SQL with advanced filters
func TranslateUrlQuery(tableName string, params url.Values) (*QueryDefinition, error) {
	if err := SanitizeIdentifier(tableName); err != nil {
		return nil, err
	}

	baseSQL := fmt.Sprintf("SELECT * FROM %s", tableName)
	whereClauses := []string{}
	parameters := []interface{}{}

	for key, values := range params {
		if key == "table" || key == "select" || key == "order" || key == "limit" || key == "offset" {
			continue // Handled separately or reserved
		}

		// Support for operators like ?age=gt.18
		val := values[0]
		operator := "="
		actualValue := val

		parts := strings.SplitN(val, ".", 2)
		if len(parts) == 2 {
			switch parts[0] {
			case "gt":
				operator = ">"
			case "lt":
				operator = "<"
			case "gte":
				operator = ">="
			case "lte":
				operator = "<="
			case "neq":
				operator = "!="
			case "like":
				operator = "LIKE"
			}
			actualValue = parts[1]
		}

		if err := SanitizeIdentifier(key); err != nil {
			continue 
		}

		parameters = append(parameters, actualValue)
		whereClauses = append(whereClauses, fmt.Sprintf("%s %s $%d", key, operator, len(parameters)))
	}

	where := ""
	if len(whereClauses) > 0 {
		where = " WHERE " + strings.Join(whereClauses, " AND ")
	}

	// 20-Pass Fix: Basic Limit/Offset Injection Prevention
	if limit := params.Get("limit"); limit != "" {
		where += fmt.Sprintf(" LIMIT %s", sanitizeNumeric(limit))
	}
	if offset := params.Get("offset"); offset != "" {
		where += fmt.Sprintf(" OFFSET %s", sanitizeNumeric(offset))
	}

	return &QueryDefinition{
		BaseSQL:    baseSQL,
		Where:      where,
		Parameters: parameters,
	}, nil
}

func sanitizeNumeric(val string) string {
	// Simple numeric validation
	for _, r := range val {
		if r < '0' || r > '9' {
			return "0"
		}
	}
	return val
}
