#!/bin/bash

# Start the Service Layer in the background
(
  cd src/ServiceLayer.API/ && func start &
)

# Start the LocalEventgridEmulator in detached mode
docker-compose -f tests/LocalEventgridEmulator/docker-compose.yml up -d

# Start the TestEventGridSubscriber in the background
(
  cd tests/e2e/TestEventGridSubscriber && func start &
)

# Wait a few seconds to let services start
sleep 15

# Run the Playwright test
(
  cd tests/e2e && npm test
)

echo "Killing all background jobs"
kill $(jobs -p)
