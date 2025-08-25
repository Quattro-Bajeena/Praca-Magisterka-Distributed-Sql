minikube start --cpus=4 --memory=8192 --disk-size=40g --driver=docker --ports=30257:30257,30080:30080
kubectl create -f ./cockroachdb-statefulset.yaml
sleep 10
# kubectl wait pod --all --for=condition=Ready --timeout=200s
kubectl create -f ./cluster-init.yaml
sleep 10
kubectl port-forward service/cockroachdb-public --address=0.0.0.0 30257:26257 30080:8080 > pf-cockroachdb-public.out &