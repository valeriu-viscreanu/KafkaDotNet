# .NET App with Kafka in Kubernetes

This project consists of:
1. An **ASP.NET Core Web API** application in .NET 8.0 that publishes messages to an Apache Kafka topic via a `/produce` POST endpoint and runs a background worker consuming messages from the same topic.
2. **Kubernetes manifests** (`k8s/`) to run a single-node Apache Kafka instance (using KRaft mode) and the .NET application inside a Kubernetes cluster.

---

## Project Structure

```text
├── k8s/
│   ├── kafka.yaml          # Kafka service and deployment in KRaft mode
│   └── dotnet-app.yaml     # .NET application service and deployment
├── src/
│   └── KafkaDotNetApp/
│       ├── Dockerfile      # Multi-stage container build file
│       ├── Program.cs      # Minimal Web API and Kafka producer registration
│       ├── KafkaConsumerWorker.cs  # Background worker consuming Kafka messages
│       ├── appsettings.json
│       └── KafkaDotNetApp.csproj
└── README.md               # This instructions file
```

---

## 1. Local Development (Optional)

If you have a local Kafka broker running on `localhost:9092`, you can run the .NET app locally using:

```bash
cd src/KafkaDotNetApp
dotnet run
```

---

## 2. Deploying to Kubernetes

Follow these steps to deploy and test the application on Kubernetes (e.g. Docker Desktop Kubernetes, Minikube, or Kind):

### Step 2.1: Deploy Kafka
Deploy the lightweight Kafka broker (KRaft mode) using the Bitnami image:

```bash
kubectl apply -f k8s/kafka.yaml
```

Check that the Kafka pod is running:
```bash
kubectl get pods -l app=kafka
```

### Step 2.2: Build the .NET App Container Image
Build the Docker container image. 

* **For general Docker Desktop K8s**:
  ```bash
  docker build -t kafka-dotnet-app:latest ./src/KafkaDotNetApp
  ```

* **For Minikube** (run inside minikube's Docker daemon so K8s can find it):
  ```bash
  # Point shell to Minikube registry
  minikube docker-env | Invoke-Expression
  
  # Build the image
  docker build -t kafka-dotnet-app:latest ./src/KafkaDotNetApp
  ```

* **For Kind** (build locally and load it into the cluster):
  ```bash
  docker build -t kafka-dotnet-app:latest ./src/KafkaDotNetApp
  kind load docker-image kafka-dotnet-app:latest
  ```

### Step 2.3: Deploy the .NET App
Deploy the .NET Web API container:

```bash
kubectl apply -f k8s/dotnet-app.yaml
```

Check that the .NET app pod is running:
```bash
kubectl get pods -l app=dotnet-app
```

---

## 3. Verifying and Testing the Application

### Step 3.1: Stream the .NET Application Logs
Open a new shell and start streaming the logs of the .NET deployment. This will allow you to watch the background consumer receive messages:

```bash
kubectl logs deployment/dotnet-app-deployment -f
```

### Step 3.2: Port Forward to the Web API
To send a message, expose the .NET App Service port to your localhost:

```bash
kubectl port-forward svc/dotnet-app-service 8080:80
```

### Step 3.3: Publish a Message
Open your browser or run a command (using PowerShell or curl) to hit the `/produce` endpoint:

**Using PowerShell:**
```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:8080/produce?message=Hello+Kafka+from+Kubernetes!"
```

**Using curl:**
```bash
curl -X POST "http://localhost:8080/produce?message=Hello+Kafka+from+Kubernetes\!"
```

### Expected Output:
1. The endpoint will return a JSON success message containing the partition and partition offset.
2. In your running log stream from Step 3.1, you should see the worker outputting:
   ```text
   info: KafkaDotNetApp.KafkaConsumerWorker[0]
         Consumed message: Value='Hello Kafka from Kubernetes!' at Offset=0
   ```
