To jest backend. Składa się z 5 kontenerów:
- webAPI - główne API programu, C# + ASP.NET Core
- cvAPI - zajmuje się wklejaniem podanej tablicy rejestracyjnej na obraz samochodu (Python + FastAPI)
- auth - służy do wydawania tokenów JWT do uwierzytelnienia i autoryzacji użytkowników
- Postgresql - baza danych
- Redis - cache aplikacji
Docelowo będzie tutaj jeszcze 6 kontener, czyli frontend.

Uruchamiacie projekt takim poleceniem:
docker-compose up --build -d
to odpali wszystkie kontenery na raz. Upewnijcie się że macie wystarczająco RAMu i miejsca na dysku bo lubi sobie trochę zjeść tych zasobów.

Wtedy macie przydatne linki:
- localhost:8080/swagger - miejsce gdzie możecie sobie testować główne API projektu
- localhost:8081/swagger - miejsce gdzie możecie sobie testować logowanie i rejestrację użytkowników.
Przed wykorzystaniem głównego API musicie się zalogować, więc polecam najpierw sobie przejść pod ten drugi link i zrobić sobie testowe konto użytkownika (zarejestrować się), później zalogować się i wystawi się specjalny token. Należy go skopiować i przejść pod pierwszy link, kliknąć sobie kłódkę i wkleić ten token. Wtedy się odblokują endpointy. Jak się zalogowaliście na operatora, to macie dostęp do endpointów operatora. Jak się zalogowaliście na zwykłego usera, to tylko do endpointów usera.

Reszta linków/kontenerów nie jest wam potrzebna, bo one są potrzebne tylko wewnętrznie i od strony frontendu nie trzeba nic więcej robić raczej z nimi.

_UWAGA_
w localhost:8081 tego lepiej nie dotykać: 
<img width="1325" height="188" alt="image" src="https://github.com/user-attachments/assets/47177fe6-1efa-4476-b0c6-3d008d2bf23a" />
to niestety musi tak być, ale traktujcie ten serwis tylko do logowania i rejestracji użytkowników. Zarządzanie pieniędzmi jest też w localhost:8080
<img width="1312" height="163" alt="image" src="https://github.com/user-attachments/assets/179c7e0b-efdb-4b6f-b291-a2562fc9f5d8" />
i tym się należy posługiwać.

