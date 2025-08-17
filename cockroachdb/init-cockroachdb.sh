minikube start --cpus=4 --memory=8192 --disk-size=40g --driver=docker --ports=30257:30257,30080:30080
kubectl create -f ./cockroachdb-statefulset.yaml
sleep 30
kubectl get pods
kubectl get pv
kubectl create -f ./cluster-init.yaml
sleep 10
kubectl get job cluster-init
kubectl get pods
minikube service cockroachdb-public --url
curl $(minikube ip):30080
# docker network connect host minikube
# kubectl port-forward service/cockroachdb-public --address=0.0.0.0 30257:26257 30080:8080 > pf-cockroachdb-public.out &