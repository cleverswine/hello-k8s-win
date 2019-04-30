FROM golang:1.11

WORKDIR /go/src/app
COPY . .

RUN go build -v -o calc .

CMD ["./calc"]