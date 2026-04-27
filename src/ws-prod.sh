#!/bin/sh
curl "<POST_API_ENDPOINT>" \
-H 'Content-Type: application/json' -X POST \
-d '{"token": "<PRODUCER_TOKEN>", "height": "'$1'", "temp": "'$2' C"}'
