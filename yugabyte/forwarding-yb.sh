kubectl --namespace yb port-forward svc/yb-masters 7000:7000 --address=0.0.0.0 > pf-yb-masters.log &
kubectl --namespace yb port-forward svc/yb-tservers 5433:5433 --address=0.0.0.0 > pf-yb-tservers.log &
kubectl --namespace yb port-forward svc/yugabyted-ui-service 15433:15433 --address=0.0.0.0 > pf-yb-yugabyted-ui.log &
echo "Forwarding initiated"
wait
echo "Forwarind ended"