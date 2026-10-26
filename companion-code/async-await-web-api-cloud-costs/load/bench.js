import http from 'k6/http';
import { check } from 'k6';

// Parameterized by environment variables so one script covers every endpoint/latency
// combination in the matrix. See README.md for how run_matrix.py drives this.
const BASE_URL = __ENV.BASE_URL || 'http://api:8080';
const ENDPOINT = __ENV.ENDPOINT || 'async-correct';
const LATENCY_MS = __ENV.LATENCY_MS || '50';
const FANOUT_N = __ENV.FANOUT_N || '10';
const ABORT_FRACTION = parseFloat(__ENV.ABORT_FRACTION || '0');
const STAGES = JSON.parse(__ENV.STAGES || '[{"duration":"15s","target":50},{"duration":"45s","target":50}]');

export const options = {
  stages: STAGES,
  thresholds: {
    http_req_duration: [`p(95)<${__ENV.P95_THRESHOLD_MS || '300'}`],
    http_req_failed: ['rate<0.01'],
  },
};

export default function () {
  const url = `${BASE_URL}/${ENDPOINT}?latencyMs=${LATENCY_MS}&n=${FANOUT_N}`;

  // For the cancellation endpoints, abort a fraction of requests mid-flight so the
  // cost of wasted work on abandoned requests is visible in the server-side metrics.
  if (ABORT_FRACTION > 0 && Math.random() < ABORT_FRACTION) {
    const params = { timeout: '5ms' };
    http.get(url, params);
    return;
  }

  const res = http.get(url);
  check(res, { 'status is 200': (r) => r.status === 200 });
}
