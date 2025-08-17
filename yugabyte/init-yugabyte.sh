minikube start --memory=8192 --cpus=4 --disk-size=40g --vm-driver=docker
kubectl create namespace yb-demo
helm repo add yugabytedb https://charts.yugabyte.com
helm repo update
helm search repo yugabytedb/yugabyte --version 2.25.2
helm install yb-demo yugabytedb/yugabyte \
--version 2.25.2 \
--set resource.master.requests.cpu=0.5,resource.master.requests.memory=0.5Gi,\
resource.tserver.requests.cpu=0.5,resource.tserver.requests.memory=0.5Gi,\
replicas.master=1,replicas.tserver=1,enableLoadBalancer=False --namespace yb-demo
helm install yb-demo yugabytedb/yugabyte --version 2.25.2 -f ./values.yaml --namespace yb-demo
helm upgrade yb-demo yugabytedb/yugabyte -f ./values.yaml
kubectl --namespace yb-demo get pods
kubectl --namespace yb-demo get services
kubectl --namespace yb-demo port-forward svc/yb-masters 7000:7000 --address=0.0.0.0 > pf-yb-masters.log &
kubectl --namespace yb-demo port-forward svc/yb-tservers 5433:5433 --address=0.0.0.0 > pf-yb-tservers.log &
kubectl --namespace yb-demo exec -it yb-tserver-0 -- sh -c "cd /home/yugabyte && ysqlsh -h yb-tserver-0 --echo-queries"
minikube tunnel > minikube-tunnel.log &


curl -O https://raw.githubusercontent.com/yugabyte/yugabyte-db/master/cloud/kubernetes/yugabyte-statefulset-rf-1.yaml
kubectl  apply --namespace='yb-demo' -f ./yugabyte-statefulset-rf-1.yaml


curl  https://raw.githubusercontent.com/yugabyte/yugabyte-db/master/cloud/kubernetes/yugabyte-statefulset-rf-1.yaml | sed 's/image: yugabytedb\/yugabyte\:latest/image: yugabytedb\/yugabyte:2025.1.0.1-b3/g' | kubectl  apply --namespace='yb-demo' -f -


helm uninstall yb-demo -n yb-demo
kubectl delete pvc --namespace yb-demo --all