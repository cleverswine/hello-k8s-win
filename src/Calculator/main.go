package main

import (
	"bytes"
	"context"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	log "github.com/sirupsen/logrus"
	"github.com/streadway/amqp"
)

// Calculation represents the status of a long running calculation
type Calculation struct {
	ID          string    `json:"id"`
	Worker      string    `json:"worker"`
	Input       string    `json:"input"`
	CallbackURL string    `json:"callbackUrl"`
	StartTime   time.Time `json:"startTime"`
	Output      string    `json:"output"`
	Status      int       `json:"status"`
	StatusTime  time.Time `json:"endTime"`
}

func init() {
	log.SetFormatter(&log.TextFormatter{DisableColors: true, FullTimestamp: true})
}

func main() {
	ctx, cancel := context.WithCancel(context.Background())
	defer cancel()

	// web server
	http.HandleFunc("/readiness", func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(200)
	})
	http.HandleFunc("/liveness", func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(200)
	})
	webServer := http.Server{Addr: ":" + envOr("PORT", "8080")}
	go func() {
		log.Info("Starting http server on port " + envOr("PORT", "8080"))
		log.Fatal(webServer.ListenAndServe())
	}()

	// rabbit connection
	var conn *amqp.Connection
	var err error
	log.Info("Connecting to RabbitMQ...")
	for i := 1; i <= 12; i++ {
		conn, err = amqp.Dial(envOr("RABBIT_URL", "amqp://rabbit:Cideloh7@localhost:5672/"))
		if err != nil {
			log.Infof("Failed to connect to RabbitMQ [%+v], will try again...", err)
			time.Sleep(time.Duration(i*1000) * time.Millisecond)
		} else {
			log.Info("Connected to RabbitMQ")
			defer conn.Close()
			break
		}
	}
	if conn == nil {
		failOnError(err, "Failed to connect to RabbitMQ")
	}
	defer conn.Close()
	ch, err := conn.Channel()
	failOnError(err, "Failed to open a receive channel")
	defer ch.Close()

	// listen for messages
	log.Info("Listening for MQ messages...")
	q, err := ch.QueueDeclare(
		envOr("QUEUE_NAME", "calc"), // name
		false,                       // durable
		false,                       // delete when usused
		false,                       // exclusive
		false,                       // no-wait
		nil,                         // arguments
	)
	failOnError(err, "Failed to declare a queue")
	msgs, err := ch.Consume(
		q.Name, // queue
		"",     // consumer
		true,   // auto-ack
		false,  // exclusive
		false,  // no-local
		false,  // no-wait
		nil,    // args
	)
	failOnError(err, "Failed to register a consumer")
	go func() {
		for d := range msgs {
			err = processCalc(d.Body)
			if err != nil {
				log.Error(err)
			}
		}
		log.Info("Quit listening for MQ messages...")
	}()

	// wait for a kill signal
	signalChan := make(chan os.Signal, 1)
	signal.Notify(signalChan, syscall.SIGINT, syscall.SIGTERM)
	<-signalChan

	// shut down
	log.Info("Shutdown signal received, exiting application...")
	webServer.Shutdown(ctx)
	time.Sleep(time.Second * 2)
}

func processCalc(b []byte) error {
	calc := Calculation{}
	err := json.Unmarshal(b, &calc)
	if err != nil {
		return err
	}
	log.Infof("Received a message with ID: %s", calc.ID)
	calc.Worker = envOr("POD", "local")
	calc.StartTime = time.Now().UTC()
	for i := 0; i < 10; i++ {
		calc.Status = i * 10
		calc.StatusTime = time.Now().UTC()
		err = statusNotify(&calc)
		if err != nil {
			return err
		}
		time.Sleep(time.Second)
	}
	calc.Output = base64.StdEncoding.EncodeToString([]byte(calc.Input))
	calc.Status = 100
	calc.StatusTime = time.Now().UTC()
	return statusNotify(&calc)
}

func statusNotify(calc *Calculation) error {
	client := http.DefaultClient
	b, err := json.Marshal(calc)
	if err != nil {
		return err
	}
	if calc.CallbackURL == "" {
		fmt.Println(string(b))
		return nil
	}
	callbackURL := envOr("WEBUI_BASE_URI", "http://localhost:5000") + calc.CallbackURL
	body := bytes.NewBuffer(b)
	req, err := http.NewRequest(http.MethodPut, callbackURL, body)
	if err != nil {
		return err
	}
	req.Header.Add("Content-Type", "application/json")
	resp, err := client.Do(req)
	if err != nil {
		return err
	}
	if resp.StatusCode != 200 {
		log.Warnf("not OK response from %s: '%s'", callbackURL, resp.Status)
	}
	log.Infof("sent status update to %s for %s - percent complete: %d", callbackURL, calc.ID, calc.Status)
	return nil
}

func failOnError(err error, msg string) {
	if err != nil {
		log.Fatalf("%s: %s", msg, err)
	}
}

func envOr(e, d string) string {
	if v, exists := os.LookupEnv(e); exists {
		return v
	}
	return d
}
