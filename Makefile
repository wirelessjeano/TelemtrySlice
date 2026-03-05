buildui:
	docker build -t telemetryslice.ui:v0.0.14 -f frontend/Dockerfile frontend

run:
	docker run -p 3001:3000 telemetryslice.ui:v0.0.1

buildapi:
	docker build -t telemetryslice.api:v0.0.1 -f backend/TelemetrySlice.App.API/Dockerfile backend

runapi:
	docker run -p 5247:8080 telemetryslice.api:v0.0.1

buildwriter:
	docker build -t telemetryslice.writer:v0.0.1 -f backend/TelemetrySlice.App.Writer/Dockerfile backend

runwriter:
	docker run -p 5248:8080 telemetryslice.writer:v0.0.1