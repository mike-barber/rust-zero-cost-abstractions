#!/bin/bash

mvn clean verify && java -jar target/benchmarks.jar -r 1 -w 1