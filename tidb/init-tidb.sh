minikube start --cpus=4 --memory=8192 --driver=docker
kubectl create -f ./crd.yaml
# helm repo add pingcap https://charts.pingcap.org/
kubectl create namespace tidb-admin
helm install --namespace tidb-admin tidb-operator pingcap/tidb-operator --version v1.6.3
kubectl get pods --namespace tidb-admin -l app.kubernetes.io/instance=tidb-operator
kubectl create namespace tidb-cluster && \
    kubectl -n tidb-cluster apply -f ./tidb-cluster.yaml
kubectl -n tidb-cluster apply -f ./tidb-dashboard.yaml
kubectl -n tidb-cluster apply -f ./tidb-monitor.yaml
watch kubectl get pod -n tidb-cluster
kubectl get service -n tidb-cluster
kubectl port-forward --address=0.0.0.0 -n tidb-cluster svc/basic-tidb 14000:4000 > pf14000.out &
kubectl port-forward --address=0.0.0.0 -n tidb-cluster svc/basic-grafana 3000:3000 > pf3000.out &
mariadb --comments -h 127.0.0.1 -P 14000 -u root
namespace=tidb-cluster
cluster_name=basic

kubectl logs pod/basic-monitor-0 -n tidb-cluster -c grafana --follow
minikube addons enable metrics-server