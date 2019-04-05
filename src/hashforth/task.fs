\ Copyright (c) 2019, Travis Bemann
\ All rights reserved.
\ 
\ Redistribution and use in source and binary forms, with or without
\ modification, are permitted provided that the following conditions are met:
\ 
\ 1. Redistributions of source code must retain the above copyright notice,
\    this list of conditions and the following disclaimer.
\ 
\ 2. Redistributions in binary form must reproduce the above copyright notice,
\    this list of conditions and the following disclaimer in the documentation
\    and/or other materials provided with the distribution.
\ 
\ 3. Neither the name of the copyright holder nor the names of its
\    contributors may be used to endorse or promote products derived from
\    this software without specific prior written permission.
\ 
\ THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
\ AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
\ IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
\ ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
\ LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
\ CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
\ SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
\ INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
\ CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
\ ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
\ POSSIBILITY OF SUCH DAMAGE.

GET-ORDER GET-CURRENT BASE @

DECIMAL
FORTH-WORDLIST 1 SET-ORDER
FORTH-WORDLIST SET-CURRENT

WORDLIST CONSTANT TASK-WORDLIST

FORTH-WORDLIST TASK-WORDLIST 2 SET-ORDER
TASK-WORDLIST SET-CURRENT

0 CONSTANT NO-WAIT
1 CONSTANT WAIT-IN
2 CONSTANT WAIT-OUT
4 CONSTANT WAIT-PRI
8 CONSTANT WAIT-TIME

VARIABLE FIRST-TASK
VARIABLE NEXT-TASK
VARIABLE CURRENT-TASK
VARIABLE AWAKE-TASK-COUNT

\ Allocate a user variable
: USER ( "name" -- ) CREATE #USER @ CELLS , 1 #USER +! DOES> @ UP @ + ;

\ Current data stack pointer
USER TASK-DATA-STACK

\ Current return stack pointer
USER TASK-RETURN-STACK

\ Current user space pointer
USER TASK-HERE

\ Current entry xt
USER TASK-ENTRY

\ Current active value (< 1 inactive, >= 1 active)
USER TASK-ACTIVE

\ Data stack base
USER TASK-SBASE

\ Return stack base
USER TASK-RBASE

\ Task wait flags
USER TASK-WAIT

\ Task wait file descriptor
USER TASK-WAIT-FD

\ Task wait time seconds
USER TASK-WAIT-TIME-S

\ Task wait time nanoseconds
USER TASK-WAIT-TIME-NS

BEGIN-STRUCTURE TASK-HEADER-SIZE
  FIELD: TASK-PREV
  FIELD: TASK-NEXT
  FIELD: TASK-UP
END-STRUCTURE

BEGIN-STRUCTURE POLL-FD-SIZE
  FIELD: POLL-FD
  FIELD: POLL-EVENTS
  FIELD: POLL-REVENTS
END-STRUCTURE

\ Access a task's user variables
: ACCESS-TASK ( xt task -- ) UP @ >R TASK-UP @ UP ! EXECUTE R> UP ! ;

\ Instantiate a new task allocated in the user space with the given data stack
\ and return stack sizes in cells, the given user space size in bytes, the
\ given local space size in cells, and the given entry point.
: NEW-TASK
  ( data-stack-size return-stack-size user-size local-size entry -- task )
  HERE TASK-HEADER-SIZE ALLOT
  0 OVER TASK-PREV !
  0 OVER TASK-NEXT !
  HERE 4 ROLL DUP #USER @ CELLS < IF
    DROP #USER @ CELLS
  THEN
  ALLOT OVER TASK-UP !
  DUP TASK-UP @ 3 ROLL DUP #USER @ < IF
    DROP #USER @
  THEN
  2DUP CELLS 0 FILL \ Fill all the user variables with zeros
  CELLS + ['] TASK-HERE 2 PICK ACCESS-TASK !
  TUCK ['] TASK-ENTRY SWAP ACCESS-TASK !
  ROT CELLS ALLOT HERE ['] TASK-DATA-STACK 2 PICK ACCESS-TASK !
  SWAP CELLS ALLOT HERE ['] TASK-RETURN-STACK 2 PICK ACCESS-TASK !
  ['] TASK-RETURN-STACK OVER ACCESS-TASK @ ['] TASK-RBASE 2 PICK ACCESS-TASK !
  ['] TASK-DATA-STACK OVER ACCESS-TASK @ ['] TASK-SBASE 2 PICK ACCESS-TASK !
  0 ['] TASK-ACTIVE 2 PICK ACCESS-TASK !
  0 ['] HANDLER 2 PICK ACCESS-TASK !
  10 ['] BASE 2 PICK ACCESS-TASK !
  0 ['] TASK-WAIT 2 PICK ACCESS-TASK !
  0 ['] TASK-WAIT-FD 2 PICK ACCESS-TASK !
  0 ['] TASK-WAIT-TIME-S 2 PICK ACCESS-TASK !
  0 ['] TASK-WAIT-TIME-NS 2 PICK ACCESS-TASK ! ;

\ Not in a task!
: X-NOT-IN-TASK SPACE ." not in a task" CR ;

\ Get whether a task is currently running.
: IN-TASK? ( -- flag ) CURRENT-TASK @ ;

\ Internal word for actually activating a task.
: (ACTIVATE-TASK) ( task -- )
  FIRST-TASK @ IF
    FIRST-TASK @ OVER TASK-NEXT !
    FIRST-TASK @ TASK-PREV @ OVER TASK-PREV !
    DUP FIRST-TASK @ TASK-PREV @ TASK-NEXT !
    DUP FIRST-TASK @ TASK-PREV !
    NEXT-TASK @ FIRST-TASK @ = IF
      DUP NEXT-TASK !
    THEN
  ELSE
    DUP DUP TASK-NEXT !
    DUP DUP TASK-PREV !
    DUP NEXT-TASK !
  THEN
  DUP FIRST-TASK !
  ['] TASK-WAIT SWAP ACCESS-TASK @ NO-WAIT = IF
    AWAKE-TASK-COUNT @ 1+ AWAKE-TASK-COUNT !
  THEN ;

\ Activate a task for execution.
: ACTIVATE-TASK ( task -- )
  ['] TASK-ACTIVE OVER ACCESS-TASK @ 1 + ['] TASK-ACTIVE 2 PICK ACCESS-TASK !
  ['] TASK-ACTIVE OVER ACCESS-TASK @ 1 = IF (ACTIVATE-TASK) ELSE DROP THEN ;

\ Force the activation of a task.
: FORCE-ACTIVATE-TASK ( task -- )
  ['] TASK-ACTIVE OVER ACCESS-TASK @ 1 < IF
    1 ['] TASK-ACTIVE 2 PICK ACCESS-TASK ! (ACTIVATE-TASK)
  ELSE
    DROP
  THEN ;

\ Last task deactivated exception
: X-LAST-TASK-DEACTIVATED ( -- ) SPACE ." last task deactivated" CR ;

\ Deactivate the last task.
: DEACTIVATE-LAST-TASK ( task -- )
  CURRENT-TASK @ = IF SP@ TASK-DATA-STACK ! RP@ TASK-RETURN-STACK ! THEN
  0 FIRST-TASK !
  0 NEXT-TASK !
  0 CURRENT-TASK !
  0 AWAKE-TASK-COUNT !
  ['] X-LAST-TASK-DEACTIVATED ?RAISE ;

\ Internal word for actually deactivating tasks.
: (DEACTIVATE-TASK) ( task -- )
  DUP TASK-NEXT @ OVER <> IF
    ['] TASK-WAIT OVER ACCESS-TASK @ NO-WAIT = IF
      AWAKE-TASK-COUNT @ 1- AWAKE-TASK-COUNT !
    THEN
    DUP NEXT-TASK @ = IF
      DUP TASK-NEXT @ NEXT-TASK !
    THEN
    DUP TASK-PREV @ OVER TASK-NEXT @ TASK-PREV !
    DUP TASK-NEXT @ OVER TASK-PREV @ TASK-NEXT !
    DUP FIRST-TASK @ = IF
      DUP TASK-NEXT @ FIRST-TASK !
    THEN
    CURRENT-TASK @ = IF
      SP@ TASK-DATA-STACK ! RP@ TASK-RETURN-STACK ! HERE TASK-HERE !
      0 CURRENT-TASK !
      PAUSE
    THEN
  ELSE
    DEACTIVATE-LAST-TASK
  THEN ;

\ Deactivate a task (remove it from execution).
: DEACTIVATE-TASK ( task -- )
  ['] TASK-ACTIVE OVER ACCESS-TASK @ 1 - ['] TASK-ACTIVE 2 PICK ACCESS-TASK !
  ['] TASK-ACTIVE OVER ACCESS-TASK @ 0 = IF (DEACTIVATE-TASK) ELSE DROP THEN ;

\ Force the deactivation of a task.
: FORCE-DEACTIVATE-TASK ( task -- )
  ['] TASK-ACTIVE OVER ACCESS-TASK @ 0 > IF
    0 ['] TASK-ACTIVE 2 PICK ACCESS-TASK ! (DEACTIVATE-TASK)
  ELSE
    DROP
  THEN ;

\ Services for multitasking
VARIABLE SYS-POLL
VARIABLE SYS-GET-MONOTONIC-TIME
VARIABLE SYS-SET-SBASE
VARIABLE SYS-SET-RBASE

\ Poll on one or more file descriptors (a return value of -1 means success and
\ a return value of 0 means error).
: POLL ( fds nfds timeout-ms -- fds-ready -1|0 ) SYS-POLL @ SYS ;

\ Get monotonic time
: GET-MONOTONIC-TIME ( -- s ns ) SYS-GET-MONOTONIC-TIME @ SYS ;

\ Set a system-wide SBASE for tracing purposes
: SET-SBASE ( sbase -- ) SYS-SET-SBASE @ SYS ;

\ Set a system-wide RBASE for tracing purposes
: SET-RBASE ( rbase -- ) SYS-SET-RBASE @ SYS ;

\ Set a task as waiting on reading a file descriptor.
: SET-WAIT-IN ( fd -- )
  TASK-WAIT @ 0 = IF AWAKE-TASK-COUNT @ 1 - AWAKE-TASK-COUNT ! THEN
  TASK-WAIT-FD ! TASK-WAIT @ WAIT-IN OR TASK-WAIT ! ;

\ Set a task as not waiting on reading a file descriptor.
: UNSET-WAIT-IN ( -- )
  0 TASK-WAIT-FD ! WAIT-IN NOT TASK-WAIT @ AND TASK-WAIT !
  TASK-WAIT @ 0 = IF AWAKE-TASK-COUNT @ 1 + AWAKE-TASK-COUNT ! THEN ;

\ Set a task as waiting on writing a file descriptor.
: SET-WAIT-OUT ( fd -- )
  TASK-WAIT @ 0 = IF AWAKE-TASK-COUNT @ 1 - AWAKE-TASK-COUNT ! THEN
  TASK-WAIT-FD ! TASK-WAIT @ WAIT-OUT OR TASK-WAIT ! ;

\ Set a task as not waiting on writing a file descriptor.
: UNSET-WAIT-OUT ( -- )
  0 TASK-WAIT-FD ! WAIT-OUT NOT TASK-WAIT @ AND TASK-WAIT !
  TASK-WAIT @ 0 = IF AWAKE-TASK-COUNT @ 1 + AWAKE-TASK-COUNT ! THEN ;

\ Set a task as waiting on reading priority data from a file descriptor.
: SET-WAIT-PRI ( fd -- )
  TASK-WAIT @ 0 = IF AWAKE-TASK-COUNT @ 1 - AWAKE-TASK-COUNT ! THEN
  TASK-WAIT-FD ! TASK-WAIT @ WAIT-PRI OR TASK-WAIT ! ;

\ Set a task as not waiting on reading priority data from a file descriptor.
: UNSET-WAIT-PRI ( -- )
  0 TASK-WAIT-FD ! WAIT-PRI NOT TASK-WAIT @ AND TASK-WAIT !
  TASK-WAIT @ 0 = IF AWAKE-TASK-COUNT @ 1 + AWAKE-TASK-COUNT ! THEN ;

\ Set a task as waiting for a given time.
: SET-WAIT-TIME ( s ns -- )
  TASK-WAIT @ 0 = IF AWAKE-TASK-COUNT @ 1 - AWAKE-TASK-COUNT ! THEN
  TASK-WAIT-TIME-NS ! TASK-WAIT-TIME-S !
  TASK-WAIT @ WAIT-TIME OR TASK-WAIT ! ;

\ Set a task as not waiting on reading priority data from a file descriptor.
: UNSET-WAIT-TIME ( -- )
  0 TASK-WAIT-TIME-NS ! 0 TASK-WAIT-TIME-S !
  WAIT-TIME NOT TASK-WAIT @ AND TASK-WAIT !
  TASK-WAIT @ 0 = IF AWAKE-TASK-COUNT @ 1 + AWAKE-TASK-COUNT ! THEN ;

\ Subtract one time from another.
: SUBTRACT-TIME ( s1 ns1 s2 ns2 -- s3 ns3 )
  3 PICK 2 PICK > IF
    2 PICK OVER >= IF
      3 ROLL ROT - ROT ROT -
    ELSE
      3 ROLL ROT - 1- ROT 1000000000 + ROT -
    THEN
  ELSE 3 PICK 2 PICK = 3 PICK 2 PICK > AND IF
    3 ROLL ROT 2DROP - 0 SWAP
  ELSE 2DROP 2DROP 0 0 THEN THEN ;

\ Sleep until a set time.
: SLEEP-UNTIL ( s ns -- )
  2DUP GET-MONOTONIC-TIME SUBTRACT-TIME 0 <> SWAP 0 <> OR IF
    2DUP SET-WAIT-TIME BEGIN
      PAUSE 2DUP GET-MONOTONIC-TIME SUBTRACT-TIME 0 = SWAP 0 = AND
    UNTIL
    UNSET-WAIT-TIME
  THEN
  2DROP ;

\ Sleep a given amount time.
: SLEEP ( s ns -- ) GET-MONOTONIC-TIME TIME+ SLEEP-UNTIL ;

\ Get whether a wait is for POLLIN, POLLOUT, or POLLPRI.
: WAIT-FD? ( wait -- flag )
  DUP WAIT-IN AND OVER WAIT-OUT AND OR SWAP WAIT-PRI AND OR ;

\ Get number of tasks that need to wait on file descriptors.
: GET-FD-WAIT-COUNT ( -- )
  FIRST-TASK @ 0 <> IF
    0 FIRST-TASK @ BEGIN
      ['] TASK-WAIT OVER ACCESS-TASK @ WAIT-FD? IF SWAP 1+ SWAP THEN
      TASK-NEXT @
    DUP FIRST-TASK @ = UNTIL
    DROP
  ELSE
    0
  THEN ;

\ Get whether polling should sleep.
: SLEEP? ( -- flag)
  FIRST-TASK @ 0 <> IF
    FIRST-TASK @ BEGIN
      ['] TASK-WAIT OVER ACCESS-TASK @ WAIT-TIME AND IF DROP TRUE EXIT THEN
      TASK-NEXT @
    DUP FIRST-TASK @ = UNTIL
    DROP FALSE
  ELSE
    FALSE
  THEN ;

\ Get the first time available for sleeping and the task with that time, or
\ 0 s 0 ns if no first time is available.
: GET-FIRST-SLEEP-TIME ( -- s ns task | 0 0 first-task )
  0 0 FIRST-TASK @ BEGIN
    ['] TASK-WAIT OVER ACCESS-TASK @ WAIT-TIME AND IF
      ROT ROT 2DROP
      ['] TASK-WAIT-TIME-S OVER ACCESS-TASK @
      ['] TASK-WAIT-TIME-NS 2 PICK ACCESS-TASK @
      ROT TASK-NEXT @ TRUE
    ELSE
      TASK-NEXT @ DUP FIRST-TASK @ =
    THEN
  UNTIL ;

\ Get the minimum of two times.
: MIN-TIME ( s1 ns1 s2 ns2 -- s3 ns3 )
  3 PICK 2 PICK = IF
    2 PICK OVER <= IF 2DROP
    ELSE ROT DROP ROT DROP THEN
  ELSE 3 PICK 2 PICK < IF 2DROP
  ELSE ROT DROP ROT DROP THEN THEN ;

\ Find the earliest sleep time.
: FIND-EARLIEST-SLEEP-TIME ( s1 ns1 task -- s2 ns2 )
  BEGIN
    ['] TASK-WAIT OVER ACCESS-TASK @ WAIT-TIME AND IF
      ROT ROT
      ['] TASK-WAIT-TIME-S 3 PICK ACCESS-TASK @
      ['] TASK-WAIT-TIME-NS 4 PICK ACCESS-TASK @ MIN-TIME
      ROT
    THEN
  TASK-NEXT @ DUP FIRST-TASK @ = UNTIL
  DROP ;

\ Convert a time into milliseconds.
: CONVERT-TIME-TO-MS ( s ns -- ms ) 1000000 / SWAP 1000 * + ;

\ Get the sleep time for polling.
: GET-SLEEP-TIME ( -- )
  SLEEP? IF
    GET-FIRST-SLEEP-TIME
    DUP FIRST-TASK @ <> IF FIND-EARLIEST-SLEEP-TIME ELSE DROP THEN
    GET-MONOTONIC-TIME SUBTRACT-TIME CONVERT-TIME-TO-MS
  ELSE
    -1
  THEN ;

\ Actually populate the polling file descriptors.
: POPULATE-POLL-FDS ( poll-fds -- )
  FIRST-TASK @ 0 <> IF
    FIRST-TASK @ BEGIN
      ['] TASK-WAIT OVER ACCESS-TASK @ WAIT-FD? IF
        2DUP ['] TASK-WAIT-FD SWAP ACCESS-TASK @ SWAP POLL-FD !
	2DUP ['] TASK-WAIT SWAP ACCESS-TASK @
	WAIT-IN WAIT-OUT OR WAIT-PRI OR AND SWAP POLL-EVENTS !
	OVER POLL-REVENTS 0 SWAP !
	SWAP POLL-FD-SIZE + SWAP
      THEN
      TASK-NEXT @
    DUP FIRST-TASK @ = UNTIL
    2DROP
  ELSE
    DROP
  THEN ;

\ The function that sleeps the system when no tasks are awake.
: DO-SLEEP ( -- )
  BEGIN
    AWAKE-TASK-COUNT @ 1 = IF
      GET-FD-WAIT-COUNT HERE OVER POLL-FD-SIZE * ALLOT
      DUP POPULATE-POLL-FDS OVER GET-SLEEP-TIME POLL POLL-FD-SIZE * NEGATE ALLOT
    THEN
    PAUSE
  AGAIN ;

\ Function to call on pause
VARIABLE 'ON-PAUSE

\ Pause count
VARIABLE PAUSE-COUNT

\ Actually handle pausing
: (PAUSE) ( -- )
  CURRENT-TASK @ IF
    SP@ TASK-DATA-STACK !
    RP@ TASK-RETURN-STACK !
    HERE TASK-HERE !
  THEN
  NEXT-TASK @ IF
    'ON-PAUSE @ ?DUP IF EXECUTE THEN
    PAUSE-COUNT @ 1 + PAUSE-COUNT !
    NEXT-TASK @ CURRENT-TASK !
    NEXT-TASK @ TASK-NEXT @ NEXT-TASK !
    CURRENT-TASK @ TASK-UP @ UP !
    TASK-HERE @ HERE!
    GET-TRACE >R FALSE SET-TRACE
    TASK-SBASE @ SET-SBASE
    TASK-RBASE @ SET-RBASE
    TASK-DATA-STACK @ SP!
    R>
    TASK-RETURN-STACK @ RP!
    SET-TRACE
    TASK-SBASE @ SBASE !
    TASK-RBASE @ RBASE !
    TASK-ENTRY @ ?DUP IF
      0 TASK-ENTRY !
      TRY ?DUP IF
        SINGLE-TASK-IO @ TRUE SINGLE-TASK-IO ! SWAP TRY IF BYE THEN
	SINGLE-TASK-IO !
      THEN
      CURRENT-TASK @ FORCE-DEACTIVATE-TASK
    THEN
  THEN ;

\ Initialize multitasking
: INIT-TASKS ( -- )
  S" POLL" SYS-LOOKUP SYS-POLL !
  S" GET-MONOTONIC-TIME" SYS-LOOKUP SYS-GET-MONOTONIC-TIME !
  S" SET-SBASE" SYS-LOOKUP SYS-SET-SBASE !
  S" SET-RBASE" SYS-LOOKUP SYS-SET-RBASE !
  ['] (PAUSE) 'PAUSE ! ;

INIT-TASKS

\ Instantiate a task for the main execution.
: NEW-MAIN-TASK ( -- task )
  HERE TASK-HEADER-SIZE ALLOT
  0 OVER TASK-PREV !
  0 OVER TASK-NEXT !
  UP @ OVER TASK-UP !
  HERE ['] TASK-HERE 2 PICK ACCESS-TASK !
  SBASE @ ['] TASK-SBASE 2 PICK ACCESS-TASK !
  RBASE @ ['] TASK-RBASE 2 PICK ACCESS-TASK !
  0 ['] TASK-ACTIVE 2 PICK ACCESS-TASK !
  0 ['] TASK-ENTRY 2 PICK ACCESS-TASK !
  0 ['] TASK-WAIT 2 PICK ACCESS-TASK !
  0 ['] TASK-WAIT-FD 2 PICK ACCESS-TASK !
  0 ['] TASK-WAIT-TIME-S 2 PICK ACCESS-TASK !
  0 ['] TASK-WAIT-TIME-NS 2 PICK ACCESS-TASK !
  DUP ACTIVATE-TASK DUP CURRENT-TASK ! PAUSE ;

NEW-MAIN-TASK CONSTANT MAIN-TASK

\ The task that sleeps the system when no tasks are awake.
1024 1024 1024 CELLS 0 ' DO-SLEEP NEW-TASK CONSTANT SLEEP-TASK
SLEEP-TASK ACTIVATE-TASK

\ Abstracting getting the current task
: CURRENT-TASK CURRENT-TASK @ ;

\ Abstracting getting the first task
: FIRST-TASK FIRST-TASK @ ;

\ Abstracting getting the next task
: NEXT-TASK NEXT-TASK @ ;

\ Abstracting getting the current number of pauses
: PAUSE-COUNT PAUSE-COUNT @ ;

BASE ! SET-CURRENT SET-ORDER
