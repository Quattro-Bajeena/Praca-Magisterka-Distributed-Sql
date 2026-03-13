minikube start --cpus 4 --memory 8192 --disk-size=40g --ports=3306:3306 #--image-mirror-country cn --registry-mirror=https://docker.mirrors.ustc.edu.cn

kubectl create namespace polardbx-operator-system

helm install --namespace polardbx-operator-system polardbx-operator https://github.com/polardb/polardbx-operator/releases/download/v1.7.0/polardbx-operator-1.7.0.tgz

# helm repo add polardbx https://polardbx-charts.oss-cn-beijing.aliyuncs.com
# helm install --namespace polardbx-operator-system polardbx-operator polardbx/polardbx-operator

kubectl get pods --namespace polardbx-operator-system

kubectl apply -f quick-start.yaml
kubectl get svc quick-start
kubectl get xstore -w

kubectl port-forward --address=0.0.0.0 svc/quick-start 3306:3306 > pf3306.out &

eval pxc=quick-start;eval user=$(kubectl get secret $pxc -o jsonpath={.data} | jq 'keys[0]'); echo "User: $user"; kubectl get secret $pxc -o jsonpath="{.data['$user']}" | base64 -d - | xargs echo "Password:"
