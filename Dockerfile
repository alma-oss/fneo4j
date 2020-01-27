FROM dcreg.service.consul/dev/development-dotnet-core-sdk-common:3.1

# build scripts
COPY ./fake.sh /fneo4j/
COPY ./build.fsx /fneo4j/
COPY ./paket.dependencies /fneo4j/
COPY ./paket.references /fneo4j/
COPY ./paket.lock /fneo4j/

# sources
COPY ./Neo4j.fsproj /fneo4j/
COPY ./src /fneo4j/src

# others
COPY ./.git /fneo4j/.git
COPY ./CHANGELOG.md /fneo4j/

WORKDIR /fneo4j

RUN \
    ./fake.sh build target Build no-clean

CMD ["./fake.sh", "build", "target", "Tests", "no-clean"]
