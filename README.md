# Task 1 - Basic microservice architecture
### Framework: C# Minimal Web API

Author: [Tymur Krasnianskyi](https://github.com/trlumph/)

## HTTP POST request workflow
![img.png](images/img.png)

## HTTP GET request flow
![img_1.png](images/img_1.png)

## Swagger Support
The Swagger UI is available at the `/swagger` endpoint. It provides a convenient way to test the API.
![img_2.png](images/img_2.png)

## CI/CD <img src="img.png" width=25, height=25/>
The project is set up with GitHub Actions to automatically build and test the project on every push to the `master` and/or `micro_basics` branches.


## Examples:
Sending some messages: (Repeat a few times to get more messages)\
![img_3.png](images/img_3.png)

LoggingService output:\
![img_5.png](images/img_5.png)

Getting all messages:\
![img_4.png](images/img_4.png)