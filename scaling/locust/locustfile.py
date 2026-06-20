import os
from locust import HttpUser, between, task


class WaterHopeUser(HttpUser):
    wait_time = between(0.2, 1.0)

    def on_start(self):
        self.token = None
        account_number = os.getenv("ACCOUNT_NUMBER", "WH-0MWXOUI0")
        password = os.getenv("PASSWORD", "123456789")
        response = self.client.post(
            "/api/auth/login",
            json={"accountNumber": account_number, "password": password},
            name="POST /api/auth/login",
        )
        if response.status_code == 200:
            self.token = response.json().get("token")

    def auth_headers(self):
        if not self.token:
            return {}
        return {"Authorization": f"Bearer {self.token}"}

    @task(4)
    def health(self):
        self.client.get("/health", name="GET /health")

    @task(3)
    def devices(self):
        self.client.get("/api/devices/my", headers=self.auth_headers(), name="GET /api/devices/my")

    @task(3)
    def consumption(self):
        self.client.get("/api/consumption/my", headers=self.auth_headers(), name="GET /api/consumption/my")

    @task(2)
    def alerts(self):
        self.client.get("/api/alerts/my", headers=self.auth_headers(), name="GET /api/alerts/my")

    @task(1)
    def tickets(self):
        self.client.get("/api/tickets/my", headers=self.auth_headers(), name="GET /api/tickets/my")
