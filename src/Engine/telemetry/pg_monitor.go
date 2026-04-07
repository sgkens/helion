package telemetry

import (
	"context"
	"github.com/jackc/pgx/v4/pgxpool"
)

type PgMonitor struct {
	Pool *pgxpool.Pool
}

type QueryStat struct {
	QueryID       string  `json:"query_id"`
	Query         string  `json:"query"`
	Calls         int64   `json:"calls"`
	TotalTimeMs   float64 `json:"total_time_ms"`
	MeanTimeMs    float64 `json:"mean_time_ms"`
	CacheHitRatio float64 `json:"cache_hit_ratio"`
}

// GetTopSlowQueries queries the pg_stat_statements extension
func (m *PgMonitor) GetTopSlowQueries(ctx context.Context, limit int) ([]QueryStat, error) {
	sql := `
		SELECT 
			queryid::text, 
			substr(query, 1, 100) as query_preview, 
			calls, 
			total_exec_time, 
			mean_exec_time,
			(shared_blks_hit::float / nullif(shared_blks_hit + shared_blks_read, 0)) * 100 as cache_hit_ratio
		FROM pg_stat_statements 
		ORDER BY mean_exec_time DESC 
		LIMIT $1;
	`
	
	rows, err := m.Pool.Query(ctx, sql, limit)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var stats []QueryStat
	for rows.Next() {
		var s QueryStat
		if err := rows.Scan(&s.QueryID, &s.Query, &s.Calls, &s.TotalTimeMs, &s.MeanTimeMs, &s.CacheHitRatio); err != nil {
			return nil, err
		}
		stats = append(stats, s)
	}
	return stats, nil
}
