docker build -t calculator:1.0.0 ./src/Calculator/
docker build -t calculator:1.0.1 ./src/Calculator/
docker build -t webui:1.0.0 ./src/WebUI/
kubectl apply -f deploy/rabbitmq.yaml -f deploy/calculator.yaml -f deploy/webui.yaml
kubectl get all