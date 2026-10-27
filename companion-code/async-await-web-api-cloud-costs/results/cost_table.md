# Cost table

Prices fetched 2026-10-27T17:59:33.083993+00:00, region: Azure eastus / AWS us-east-1, on-demand rates only.

Azure App Service: B1 (1 vCPU, 1.75GB), the closest published tier to 1 vCPU / 2GB. AWS Fargate: exact 1 vCPU / 2GB task. AWS Lightsail: the "2GB" standard Linux bundle (2.0 vCPU, 2GB) is the closest published bundle by RAM, but it's 2 vCPU, not 1, so it's a generous comparison point for Lightsail, not an exact match.

Headroom factor: 0.65 (instances = ceil(target_rps / (sustainable_rps_per_instance * headroom))).

## Target: 1,000 RPS

| Endpoint | Stub latency | Sustainable RPS/instance | Instances needed | Azure App Service $/mo | AWS Fargate $/mo | AWS Lightsail $/mo | Ratio vs best |
|---|---|---|---|---|---|---|---|
| async-correct | 50ms | 3298.1 | 1 | $12 | $36 | $12 | 1.0x |
| async-correct | 200ms | 1861.4 | 1 | $12 | $36 | $12 | 1.0x |
| fanout-unbounded | 50ms | 500.6 | 4 | $50 | $144 | $47 | 4.0x |
| sync-over-async | 50ms | 367.4 | 5 | $62 | $180 | $59 | 5.0x |
| fanout-bounded | 50ms | 281.6 | 6 | $75 | $216 | $71 | 6.0x |
| fanout-unbounded | 200ms | 199.0 | 8 | $99 | $289 | $94 | 8.0x |
| sync-over-async | 200ms | 135.0 | 12 | $149 | $433 | $141 | 12.0x |

## Target: 2,000 RPS

| Endpoint | Stub latency | Sustainable RPS/instance | Instances needed | Azure App Service $/mo | AWS Fargate $/mo | AWS Lightsail $/mo | Ratio vs best |
|---|---|---|---|---|---|---|---|
| async-correct | 50ms | 3298.1 | 1 | $12 | $36 | $12 | 1.0x |
| async-correct | 200ms | 1861.4 | 2 | $25 | $72 | $24 | 2.0x |
| fanout-unbounded | 50ms | 500.6 | 7 | $87 | $252 | $82 | 7.0x |
| sync-over-async | 50ms | 367.4 | 9 | $112 | $325 | $106 | 9.0x |
| fanout-bounded | 50ms | 281.6 | 11 | $137 | $397 | $130 | 11.0x |
| fanout-unbounded | 200ms | 199.0 | 16 | $199 | $577 | $188 | 16.0x |
| sync-over-async | 200ms | 135.0 | 23 | $286 | $830 | $271 | 23.0x |

## Target: 5,000 RPS

| Endpoint | Stub latency | Sustainable RPS/instance | Instances needed | Azure App Service $/mo | AWS Fargate $/mo | AWS Lightsail $/mo | Ratio vs best |
|---|---|---|---|---|---|---|---|
| async-correct | 50ms | 3298.1 | 3 | $37 | $108 | $35 | 1.0x |
| async-correct | 200ms | 1861.4 | 5 | $62 | $180 | $59 | 1.7x |
| fanout-unbounded | 50ms | 500.6 | 16 | $199 | $577 | $188 | 5.3x |
| sync-over-async | 50ms | 367.4 | 21 | $261 | $757 | $247 | 7.0x |
| fanout-bounded | 50ms | 281.6 | 28 | $348 | $1,010 | $330 | 9.3x |
| fanout-unbounded | 200ms | 199.0 | 39 | $484 | $1,407 | $459 | 13.0x |
| sync-over-async | 200ms | 135.0 | 57 | $708 | $2,056 | $671 | 19.0x |

## Target: 20,000 RPS

| Endpoint | Stub latency | Sustainable RPS/instance | Instances needed | Azure App Service $/mo | AWS Fargate $/mo | AWS Lightsail $/mo | Ratio vs best |
|---|---|---|---|---|---|---|---|
| async-correct | 50ms | 3298.1 | 10 | $124 | $361 | $118 | 1.0x |
| async-correct | 200ms | 1861.4 | 17 | $211 | $613 | $200 | 1.7x |
| fanout-unbounded | 50ms | 500.6 | 62 | $770 | $2,236 | $730 | 6.2x |
| sync-over-async | 50ms | 367.4 | 84 | $1,043 | $3,030 | $989 | 8.4x |
| fanout-bounded | 50ms | 281.6 | 110 | $1,366 | $3,967 | $1,295 | 11.0x |
| fanout-unbounded | 200ms | 199.0 | 155 | $1,925 | $5,591 | $1,825 | 15.5x |
| sync-over-async | 200ms | 135.0 | 228 | $2,832 | $8,223 | $2,685 | 22.8x |
