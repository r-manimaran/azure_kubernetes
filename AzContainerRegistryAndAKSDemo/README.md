## Build and Create docker Image
```bash
# Build the image with version tag
> docker build . tag webapi:v1
```

> Login to the Azure Container registry

1. Using environment Variable:
# Store password in environment variable
export ACR_PASSWORD=$(az acr credential show --name maranacr --query "passwords[0].value" -o tsv)

# Use the environment variable for login
echo $ACR_PASSWORD | docker login maranacr.azurecr.io -u maranacr --password-stdin

2. using a script file:
```bash
#!/bin/bash
PASSWORD=$(az acr credential show --name maranacr --query "passwords[0].value" -o tsv)
echo $PASSWORD | docker login maranacr.azurecr.io -u maranacr --password-stdin
```
3. using the PowerShell
```powershell
$password = az acr credential show --name maranacr --query "passwords[0].value" -o tsv
$password | docker login maranacr.azurecr.io -u maranacr --password-stdin

```
The --password-stdin flag is more secure than passing the password directly on the command line as it won't show up in your command history

## Push images to Azure Container Registry (ACR)

1. First build your image with proper tagging

```bash
# Build with version tag
> docker build -t webapi:v1

# tag the image for ACR with both latest and version tags
> docker tag webapi:v1 maranacr.azurecr.io/webapi:v1
> docker tag webapi:v1 maranacr.azurecr.io/webapi:latest
```
2. Secure Login tp ACR using Password-stdin:

```bash
# Get ACR Password securely

export ACR_PASSWORD=$(az acr credential show --name maranacr --query "passwords[0].value" -o tsv)

# Login to ACR
echo $ACR_PASSWORD | docker login maranacr.azurecr.io -u maranacr --password-stdin

```
3. Push the image

```bash
# Push both version and latest tags
docker push maranacr.azurecr.io/webapi:v1
docker push maranacr.azurecr.io/webapi:latest

```


![alt text](image.png)

![alt text](image-3.png)

4. Check and list the repositories after push
 - using Azure CLI
```bash

# List all repositories
az acr repository list --name maranacr --output table

# List tags for a specific repository
az acr repository show-tags --name maranacr --repository webapi --output table

# Show detailed information about tags
az acr repository show --name maranacr --repository webapi

```
![alt text](image-1.png)

- Using Docker commands
```bash
# List local images with ACR tags
docker images maranacr.azurecr.io/*

# List specific repository images
docker images maranacr.azurecr.io/webapi
```
![alt text](image-2.png)

- All the above steps are done in the single script file (build-and-push.sh)

## Best Practices 
```bash
# Scan images for vulnerabilites before pushing
docker scan maranacr.azurecr.io/webapi:v1

# Use Image sigining for security
docker trust sign maranacr.azurecr.io/webapi:v1

# Enable Azure Container Registry content trust
az acr config content-trust update --status enabled -n maranacr

# Use Azure RBAC for access control
az role assignment create \
    --role AcrPush \
    --assignee <user-or-service-principal-id> \
    --scope /subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.ContainerRegistry/registries/maranacr

```

## Use the build-and-push.sh
 1. Using Git Bash
 ```bash
 # Navigate to your project directory
cd /c/Maran/Study/Git/Dotnet/azure-dotnet/AzContainerRegistryAndAKSDemo

# Make the script executable
chmod +x build-and-push.sh

# Run the script
./build-and-push.sh
```

## Run Kubernetes locally using image from Azure Container Registry
1. Create the deployment.yml file
2. Create the Kubernetes secret for ACR authentication
```bash
# create the secrets
kubectl create secret docker-registry acr-secret \
  --docker-server=maranacr.azurecr.io \
  --docker-username=maranacr \
  --docker-password=<your-acr-password>
```

Instead of above password paste option, create a script file create_acr_secret.sh. Running the script will create the secret.

- To run the create_acr_secret.sh file do,

```bash
chmod +x create_acr_secret.sh

./create_acr_secret.sh
```
![alt text](image-4.png)

- To check the created secrets, we can run the below scripts
```bash
# get the secrets
kubectl get secrets

# Describe the secret
kubectl describe secret acr-secret

# to delete the existing secret
kubectl delete secret acr-secret
```
![alt text](image-5.png)

### Create the Connection string secret
```bash
kubectl create secret generic azure-sql-secret \
 --from-literal=ConnectionStrings__AzureSql="Server=tcp:maranazsql.database.windows.net,1433;Initial Catalog=Demodb;Persist Security Info=False;User ID=demoadmin;Password=passwordhere;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
 ```
 ![alt text](image-6.png)

 - Update the deployment.yml file to pass the connection string


 ![alt text](image-7.png)

![alt text](image-8.png)

- Scale the replicas up and down

```bash
# Scale to 0
kubectl scale deployment webapi-deployment --replicas=0

# Scale back to desired number
kubectl scale deployment webapi-deployment --replicas=2
```
Tried the below command
```bash
kubectl scale deployment webapi-deployment --replicas=3
```
![alt text](image-9.png)

Tried to scale down using 
```bash
kubectl scale deployment webapi-deployment --replicas=2
```
![alt text](image-10.png)

```bash
kubectl scale deployment webapi-deployment --replicas=0
```
![alt text](image-11.png)
 

 ![alt text](image-12.png)