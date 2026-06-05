# .NET App with Kafka in Kubernetes (Tandem Setup)

This project consists of two separate services running in tandem:
1. **KafkaProducer** (`src/KafkaProducer`): An ASP.NET Core Web API that exposes a `/produce` POST endpoint to publish messages.
2. **KafkaConsumer** (`src/KafkaConsumer`): A background .NET worker service that consumes messages from the Kafka topic and prints them to the console logs.

---

## Project Structure

```text
├── k8s/
│   ├── kafka.yaml          # Kafka service and deployment (KRaft mode)
│   └── dotnet-app.yaml     # Separate Producer and Consumer deployments
├── src/
│   ├── KafkaProducer/      # Web API Producer
│   │   ├── Dockerfile
│   │   ├── Program.cs
│   │   └── ...
│   └── KafkaConsumer/      # Worker Service Consumer
│       ├── Dockerfile
│       ├── Program.cs
│       └── ...
└── README.md
```

---

## 1. Local Development (Optional)

You can run both apps locally if you have Kafka running on `localhost:9092`.

In shell 1 (Consumer):
```bash
cd src/KafkaConsumer
dotnet run
```

In shell 2 (Producer):
```bash
cd src/KafkaProducer
dotnet run
```

---

## 2. Deploying to Kubernetes

### Step 2.1: Deploy Kafka
Deploy the lightweight Kafka broker (KRaft mode):

```bash
kubectl apply -f k8s/kafka.yaml
```

### Step 2.2: Build the Container Images
Build the Docker images for both services:

```bash
# Build Producer
docker build -t kafka-producer-app:latest ./src/KafkaProducer

# Build Consumer
docker build -t kafka-consumer-app:latest ./src/KafkaConsumer
```

*Note: If you are using Minikube, remember to run `minikube docker-env | Invoke-Expression` (or `eval $(minikube docker-env)` on bash) before building, or use `minikube image build`. If using Kind, load them using `kind load docker-image <image_name>:latest`.*

### Step 2.3: Deploy the Apps
Deploy the producer and consumer:

```bash
kubectl apply -f k8s/dotnet-app.yaml
```

---

## 3. Testing the Tandem Services

1. **Watch Consumer Logs**:
   ```bash
   kubectl logs deployment/dotnet-consumer-deployment -f
   ```

2. **Port Forward to the Producer API**:
   ```bash
   kubectl port-forward svc/dotnet-producer-service 8080:80
   ```

3. **Publish a Message**:
   Send a POST request to the producer endpoint:
   
   *PowerShell*:
   ```powershell
   Invoke-RestMethod -Method Post -Uri "http://localhost:8080/produce?message=Hello+Tandem+Kafka!"
   ```
   
   *Curl*:
   ```bash
   curl -X POST "http://localhost:8080/produce?message=Hello+Tandem+Kafka\!"
   ```

You will see the producer return a success response, and the consumer log stream will print:
```text
info: KafkaConsumer.KafkaConsumerWorker[0]
      Consumed message: Value='Hello Tandem Kafka!' at Offset=0
```
