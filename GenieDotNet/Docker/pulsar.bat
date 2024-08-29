docker run -it ^
-p 6650:6650 ^
-p 8080:8080 ^
apachepulsar/pulsar:3.3.0 ^
bin/pulsar standalone

REM --mount source=pulsardata,target=/pulsar/data ^
REM --mount source=pulsarconf,target=/pulsar/conf ^