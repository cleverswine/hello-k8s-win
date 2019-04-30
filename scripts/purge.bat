kubectl delete -f deploy/webui.yaml -f deploy/calculator.yaml -f deploy/rabbitmq.yaml
docker rmi webui:1.0.0
docker rmi calculator:1.0.1
docker rmi calculator:1.0.0
kubectl get all
