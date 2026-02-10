minikube start --kubernetes-version=v1.25.8 --cpus=4 --memory=11000 --disk-size=32g
git clone https://github.com/vitessio/vitess
git checkout release-16.0
cd vitess/examples/operator
kubectl apply -f operator.yaml
kubectl apply -f 101_initial_cluster.yaml

./pf.sh &
alias vtctldclient="vtctldclient --server=localhost:15999"
alias vtctlclient="vtctlclient --server=localhost:15999"
alias mysql="mysql -h 127.0.0.1 -P 15306 -u user"


kubectl port-forward -n example --address 0.0.0.0 "$(kubectl get service -n example --selector="planetscale.com/component=vtctld" -o name | head -n1)" 15000 15999 &
process_id1=$!
kubectl port-forward -n example --address 0.0.0.0 "$(kubectl get service -n example --selector="planetscale.com/component=vtgate,!planetscale.com/cell" -o name | head -n1)" 15306:3306 &
process_id2=$!
kubectl port-forward -n example --address 0.0.0.0 "$(kubectl get service -n example --selector="planetscale.com/component=vtadmin" -o name | head -n1)" 14000:15000 14001:15001 &
process_id3=$!
sleep 2
echo "You may point your browser to http://localhost:15000, use the following aliases as shortcuts:"
echo 'alias vtctldclient="vtctldclient --server=localhost:15999 --logtostderr"'
echo 'alias mysql="mysql -h 127.0.0.1 -P 15306 -u user"'
echo "Hit Ctrl-C to stop the port forwards"
wait $process_id1
wait $process_id2
wait $process_id3
