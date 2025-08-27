cd /home/mateusz/distributedsql/yugabyte
minikube start --memory=8192 --cpus=4 --disk-size=40g --vm-driver=docker --ports=15433:15433,5433:5433,7000:7000
kubectl create namespace yb
helm install yb yugabytedb/yugabyte --version 2.25.2 -f ./values.yaml --namespace yb
sleep 10
kubectl wait pod --namespace yb --all --for=condition=Ready --timeout=200s
sleep 30
kubectl --namespace yb exec -it yb-tserver-0 -- sh -c "cd /home/yugabyte && echo 'CREATE DATABASE yb;GRANT ALL ON DATABASE yb to yugabyte;' | ysqlsh -h yb-tserver-0 --echo-queries"
# kubectl --namespace yb port-forward svc/yb-masters 7000:7000 --address=0.0.0.0 > pf-yb-masters.log &
# kubectl --namespace yb port-forward svc/yb-tservers 5433:5433 --address=0.0.0.0 > pf-yb-tservers.log &
# kubectl --namespace yb port-forward svc/yugabyted-ui-service 15433:15433 --address=0.0.0.0 > pf-yb-yugabyted-ui.log &
# echo "Initialized Port forwarding yugabyte"
# wait 