#!/bin/bash

kubectl logs -f $(kubectl get po --selector=app=$1 --output=jsonpath={.items..metadata.name})
