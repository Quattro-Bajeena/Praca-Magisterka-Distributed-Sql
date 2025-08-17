minikube start --cpus=4 --memory=8192 --disk-size=40g --driver=docker
helm repo add voltdb https://voltdb-kubernetes-charts.storage.googleapis.com
kubectl create secret docker-registry dockerio-registry --docker-username=paraon --docker-email=motmen001@gmail.com --docker-password=
helm install mydb voltdb/voltdb        \
   --set global.voltdbVersion=13.2.0     \
   --set global.image.credentials.username=paraon \
   --set global.image.credentials.password=Adelinold001 \
   --set-file cluster.config.licenseXMLFile=mateusz.oleszek-2025-11-21.xml 



cd /home/mateusz/distributedsql/voltdb/TollCollectDemo/dev-edition-app/target/dev-edition-app-1.0-SNAPSHOT/dev-edition-app
docker compose up




docker.io/voltdb/voltdb-operator
https://www.docker.com/voltdb/voltdb-operator

git clone https://github.com/VoltDB/TollCollectDemo.git
cd TollCollectDemo
./mvnw clean package
cd target/dev-edition-app-1.0-SNAPSHOT/dev-edition-app
docker compose up

cd TollCollectClient/target
java -jar TollCollectClient-1.0-SNAPSHOT.jar


mv docker-compose-3dbnodes.yaml docker-compose.yaml