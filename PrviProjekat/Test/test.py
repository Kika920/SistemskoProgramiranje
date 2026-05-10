import urllib.request
import threading
import time

URL = "http://localhost:8080/?creator=Rembrandt"

THREADS = 100

start_barrier = threading.Barrier(THREADS)

success = 0
errors = 0

lock = threading.Lock()


def send_request(i):
    global success, errors

    try:
       
        start_barrier.wait()

        start = time.time()

        response = urllib.request.urlopen(URL)

        duration = time.time() - start

        with lock:
            success += 1

        print(f"[{i}] STATUS={response.status} TIME={duration:.2f}s")

    except Exception as e:
        with lock:
            errors += 1

        print(f"[{i}] ERROR={e}")


threads = []

start_total = time.time()

for i in range(THREADS):
    t = threading.Thread(target=send_request, args=(i,))
    threads.append(t)
    t.start()

for t in threads:
    t.join()

total = time.time() - start_total

print()
print("========== REZULTAT ==========")
print(f"THREADS : {THREADS}")
print(f"SUCCESS : {success}")
print(f"ERRORS  : {errors}")
print(f"TOTAL   : {total:.2f}s")