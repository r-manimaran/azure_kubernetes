# Set variables
$ACR_NAME = "maranacr"
$IMAGE_NAME = "webapi"
$VERSION = "v1"

# Build the image
Write-Host "Building image..."
docker build -t $IMAGE_NAME`:$VERSION .

# Tag images
Write-Host "Tagging images..."
docker tag $IMAGE_NAME`:$VERSION $ACR_NAME.azurecr.io/$IMAGE_NAME`:$VERSION
docker tag $IMAGE_NAME`:$VERSION $ACR_NAME.azurecr.io/$IMAGE_NAME`:latest

# Login to ACR
Write-Host "Logging into ACR..."
$ACR_PASSWORD = az acr credential show --name $ACR_NAME --query "passwords[0].value" -o tsv
$ACR_PASSWORD | docker login $ACR_NAME.azurecr.io -u $ACR_NAME --password-stdin

# Push images
Write-Host "Pushing images..."
docker push $ACR_NAME.azurecr.io/$IMAGE_NAME`:$VERSION
docker push $ACR_NAME.azurecr.io/$IMAGE_NAME`:latest

# Cleanup
Write-Host "Cleaning up local images..."
docker rmi $ACR_NAME.azurecr.io/$IMAGE_NAME`:$VERSION
docker rmi $ACR_NAME.azurecr.io/$IMAGE_NAME`:latest
docker rmi $IMAGE_NAME`:$VERSION

Write-Host "Process completed successfully!"
