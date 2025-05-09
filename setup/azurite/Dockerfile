FROM python:3.13-slim

COPY ./setup/azurite .
RUN pip install --no-cache-dir azure-storage-blob==12.22.0 && pip install azure-storage-queue
CMD ["python3", "-u", "./azurite-setup.py"]
