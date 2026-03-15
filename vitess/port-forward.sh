kubectl port-forward -n example --address 0.0.0.0 "$(kubectl get service -n example --selector="planetscale.com/component=vtctld" -o name | head -n1)" 15000:15999 &
process_id1=$!
kubectl port-forward -n example --address 0.0.0.0 "$(kubectl get service -n example --selector="planetscale.com/component=vtgate" -o name | head -n1)" 15306:3306 &
# kubectl port-forward -n example --address 0.0.0.0 "$(kubectl get service -n example --selector="planetscale.com/component=vtgate,!planetscale.com/cell" -o name | head -n1)" 15306:3306 &
process_id2=$!
kubectl port-forward -n example --address 0.0.0.0 "$(kubectl get service -n example --selector="planetscale.com/component=vtadmin" -o name | head -n1)" 14000:15000 14001:15001 &
process_id3=$!
sleep 2

wait $process_id1
wait $process_id2
wait $process_id3