# Cost table

Prices fetched 2026-10-26T22:26:52.540036+00:00, region: Azure eastus / AWS us-east-1, on-demand rates only.

Headroom factor: 0.65 (instances = ceil(target_rps / (sustainable_rps_per_instance * headroom))).

## Target: 500 RPS

| Endpoint | Stub latency | Sustainable RPS/instance | Instances needed | Azure $/mo | AWS Fargate $/mo | Ratio vs best |
|---|---|---|---|---|---|---|
| async-correct | 50ms | 3298.1 | 1 | $12 | $36 | 1.0x |
| async-correct | 200ms | 1861.4 | 1 | $12 | $36 | 1.0x |
| fanout-unbounded | 50ms | 500.6 | 2 | $25 | $72 | 2.0x |
| sync-over-async | 50ms | 367.4 | 3 | $37 | $108 | 3.0x |
| fanout-bounded | 50ms | 281.6 | 3 | $37 | $108 | 3.0x |
| fanout-unbounded | 200ms | 199.0 | 4 | $50 | $144 | 4.0x |
| sync-over-async | 200ms | 135.0 | 6 | $75 | $216 | 6.0x |

## Target: 2,000 RPS

| Endpoint | Stub latency | Sustainable RPS/instance | Instances needed | Azure $/mo | AWS Fargate $/mo | Ratio vs best |
|---|---|---|---|---|---|---|
| async-correct | 50ms | 3298.1 | 1 | $12 | $36 | 1.0x |
| async-correct | 200ms | 1861.4 | 2 | $25 | $72 | 2.0x |
| fanout-unbounded | 50ms | 500.6 | 7 | $87 | $252 | 7.0x |
| sync-over-async | 50ms | 367.4 | 9 | $112 | $325 | 9.0x |
| fanout-bounded | 50ms | 281.6 | 11 | $137 | $397 | 11.0x |
| fanout-unbounded | 200ms | 199.0 | 16 | $199 | $577 | 16.0x |
| sync-over-async | 200ms | 135.0 | 23 | $286 | $830 | 23.0x |

## Target: 10,000 RPS

| Endpoint | Stub latency | Sustainable RPS/instance | Instances needed | Azure $/mo | AWS Fargate $/mo | Ratio vs best |
|---|---|---|---|---|---|---|
| async-correct | 50ms | 3298.1 | 5 | $62 | $180 | 1.0x |
| async-correct | 200ms | 1861.4 | 9 | $112 | $325 | 1.8x |
| fanout-unbounded | 50ms | 500.6 | 31 | $385 | $1,118 | 6.2x |
| sync-over-async | 50ms | 367.4 | 42 | $522 | $1,515 | 8.4x |
| fanout-bounded | 50ms | 281.6 | 55 | $683 | $1,984 | 11.0x |
| fanout-unbounded | 200ms | 199.0 | 78 | $969 | $2,813 | 15.6x |
| sync-over-async | 200ms | 135.0 | 114 | $1,416 | $4,112 | 22.8x |
