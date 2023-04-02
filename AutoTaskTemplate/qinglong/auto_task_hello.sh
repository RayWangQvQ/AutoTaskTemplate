#!/usr/bin/env bash
# new Env("bili每日任务")
# cron 0 9 * * * auto_task_hello.sh
. auto_task_base.sh

cd ./src/AutoTaskTemplate

export AutoTaskTemplate_Run=Hello && \
dotnet run
