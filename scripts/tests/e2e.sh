#!/bin/bash

# Start the Service Layer in background
cd src/ServiceLayer.API/
func start &
cd ../..

# Start the LocalEventgridEmulator in detached mode
cd tests/LocalEventgridEmulator
docker-compose up -d
cd ../..

# Start the TestEventGridSubscriber in background
cd tests/e2e/TestEventGridSubscriber
func start &
cd ..

# Optional: Wait a few seconds to let services start
sleep 15

# Run the Playwright test
npm test

echo "Killing all background jobs"
kill $(jobs -p)
