#!/usr/bin/env bash

set -euo pipefail

: "${AWS_ACCESS_KEY_ID?}" "${AWS_SECRET_ACCESS_KEY?}" "${AWS_SESSION_TOKEN?}"
: "${S3_BUCKET:=downloads-cdn-eu-central-1-prod}"
ROOT_BUCKET_KEY="SonarLint-for-VisualStudio/dogfood"
dogfood_site_dir="$1"

echo "Upload from $dogfood_site_dir to s3://$S3_BUCKET/$ROOT_BUCKET_KEY/"
aws s3 sync --delete "$dogfood_site_dir" "s3://$S3_BUCKET/$ROOT_BUCKET_KEY/"
