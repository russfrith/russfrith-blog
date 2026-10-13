// This runs in the browser, not inside the Docker network, so it has to be the host port
// Docker Compose publishes - the API's compose service name ("api") isn't resolvable here.
export const API_BASE_URL = 'http://localhost:5268';
