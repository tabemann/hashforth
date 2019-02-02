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

BEGIN-STRUCTURE TASK-HEADER-SIZE
  FIELD: TASK-PREV
  FIELD: TASK-NEXT
  FIELD: TASK-DATA-STACK
  FIELD: TASK-RETURN-STACK
  FIELD: TASK-SBASE
  FIELD: TASK-RBASE
  FIELD: TASK-LOCAL
  FIELD: TASK-ENTRY
  FIELD: TASK-WAIT
  FIELD: TASK-WAIT-FD
  FIELD: TASK-WAIT-TIME-S
  FIELD: TASK-WAIT-TIME-NS
  FIELD: TASK-HANDLER
  FIELD: TASK-ACTIVE
END-STRUCTURE

BEGIN-STRUCTURE POLL-FD-SIZE
  FIELD: POLL-FD
  FIELD: POLL-EVENTS
  FIELD: POLL-REVENTS
END-STRUCTURE

\ Instantiate a new task allocated in the user space with the given data stack
\ and return stack sizes in cells, the given local space size in bytes, and
\ the given entry point.
: NEW-TASK ( data-stack-size return-stack-size local-size entry -- task )
  HERE TASK-HEADER-SIZE ALLOT
  0 OVER TASK-PREV !
  0 OVER TASK-NEXT !
  TUCK TASK-ENTRY !
  HERE ROT ALLOT SWAP TUCK TASK-LOCAL !
  SWAP CELLS ALLOT HERE OVER TASK-RETURN-STACK !
  SWAP CELLS ALLOT HERE OVER TASK-DATA-STACK !
  DUP TASK-RETURN-STACK @ OVER TASK-RBASE !
  DUP TASK-DATA-STACK @ OVER TASK-SBASE !
  0 OVER TASK-WAIT !
  0 OVER TASK-WAIT-FD !
  0 OVER TASK-WAIT-TIME-S !
  0 OVER TASK-WAIT-TIME-NS !
  0 OVER TASK-HANDLER !
  0 OVER TASK-ACTIVE ! ;

\ Not in a task!
: X-NOT-IN-TASK SPACE S" not in a task" CR ;

\ Get whether a task is currently running.
: IN-TASK? ( -- flag ) CURRENT-TASK @ ;

\ Get the task-local space pointer.
: THERE ( -- addr ) IN-TASK? AVERTS X-NOT-IN-TASK CURRENT-TASK @ TASK-LOCAL @ ;

\ Set the task-local space pointer.
: THERE! ( addr -- ) IN-TASK? AVERTS X-NOT-IN-TASK CURRENT-TASK @ TASK-LOCAL ! ;

\ Allot space (or move the pointer back) in the task-local space pointer.
: TALLOT ( offset -- ) THERE + THERE! ;

\ Activate a task for execution.
: ACTIVATE-TASK ( task -- )
  DUP TASK-ACTIVE @ 1 + OVER TASK-ACTIVE !
  DUP TASK-ACTIVE @ 1 = IF
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
    TASK-WAIT @ NO-WAIT = IF
      AWAKE-TASK-COUNT @ 1+ AWAKE-TASK-COUNT !
    THEN
  ELSE
    DROP
  THEN ;

\ Last task deactivated exception
: X-LAST-TASK-DEACTIVATED ( -- ) SPACE ." last task deactivated" CR ;

\ Deactivate the last task.
: DEACTIVATE-LAST-TASK ( task -- )
  CURRENT-TASK @ = IF
    HANDLER CURRENT-TASK @ TASK-HANDLER !
    SP@ CURRENT-TASK @ TASK-DATA-STACK !
    RP@ CURRENT-TASK @ TASK-RETURN-STACK !
  THEN
  0 FIRST-TASK !
  0 NEXT-TASK !
  0 CURRENT-TASK !
  0 AWAKE-TASK-COUNT !
  ['] X-LAST-TASK-DEACTIVATED ?RAISE ;

\ Deactivate a task (remove it from execution).
: DEACTIVATE-TASK ( task -- )
  DUP TASK-ACTIVE @ 1 - OVER TASK-ACTIVE !
  DUP TASK-ACTIVE @ 0 = IF
    DUP TASK-NEXT @ OVER <> IF
      DUP TASK-WAIT @ NO-WAIT = IF
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
        HANDLER CURRENT-TASK @ TASK-HANDLER !
        SP@ CURRENT-TASK @ TASK-DATA-STACK !
        RP@ CURRENT-TASK @ TASK-RETURN-STACK !
        0 CURRENT-TASK !
        PAUSE
      THEN
    ELSE
      DEACTIVATE-LAST-TASK
    THEN
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
: SET-WAIT-IN ( fd task -- )
  DUP TASK-WAIT @ 0 = IF AWAKE-TASK-COUNT @ 1 - AWAKE-TASK-COUNT ! THEN
  TUCK TASK-WAIT-FD ! DUP TASK-WAIT @ WAIT-IN OR SWAP TASK-WAIT ! ;

\ Set a task as not waiting on reading a file descriptor.
: UNSET-WAIT-IN ( task -- )
  0 OVER TASK-WAIT-FD ! WAIT-IN NOT OVER TASK-WAIT @ AND OVER TASK-WAIT !
  TASK-WAIT @ 0 = IF AWAKE-TASK-COUNT @ 1 + AWAKE-TASK-COUNT ! THEN ;

\ Set a task as waiting on writing a file descriptor.
: SET-WAIT-OUT ( fd task -- )
  DUP TASK-WAIT @ 0 = IF AWAKE-TASK-COUNT @ 1 - AWAKE-TASK-COUNT ! THEN
  TUCK TASK-WAIT-FD ! DUP TASK-WAIT @ WAIT-OUT OR SWAP TASK-WAIT ! ;

\ Set a task as not waiting on writing a file descriptor.
: UNSET-WAIT-OUT ( task -- )
  0 OVER TASK-WAIT-FD ! WAIT-OUT NOT OVER TASK-WAIT @ AND OVER TASK-WAIT !
  TASK-WAIT @ 0 = IF AWAKE-TASK-COUNT @ 1 + AWAKE-TASK-COUNT ! THEN ;

\ Set a task as waiting on reading priority data from a file descriptor.
: SET-WAIT-PRI ( fd task -- )
  DUP TASK-WAIT @ 0 = IF AWAKE-TASK-COUNT @ 1 - AWAKE-TASK-COUNT ! THEN
  TUCK TASK-WAIT-FD ! DUP TASK-WAIT @ WAIT-PRI OR SWAP TASK-WAIT ! ;

\ Set a task as not waiting on reading priority data from a file descriptor.
: UNSET-WAIT-PRI ( task -- )
  0 OVER TASK-WAIT-FD ! WAIT-PRI NOT OVER TASK-WAIT @ AND OVER TASK-WAIT !
  TASK-WAIT @ 0 = IF AWAKE-TASK-COUNT @ 1 + AWAKE-TASK-COUNT ! THEN ;

\ Set a task as waiting for a given time.
: SET-WAIT-TIME ( s ns task -- )
  DUP TASK-WAIT @ 0 = IF AWAKE-TASK-COUNT @ 1 - AWAKE-TASK-COUNT ! THEN
  TUCK TASK-WAIT-TIME-NS ! TUCK TASK-WAIT-TIME-S !
  DUP TASK-WAIT @ WAIT-TIME OR SWAP TASK-WAIT ! ;

\ Set a task as not waiting on reading priority data from a file descriptor.
: UNSET-WAIT-TIME ( task -- )
  0 OVER TASK-WAIT-TIME-NS ! 0 OVER TASK-WAIT-TIME-S !
  WAIT-TIME NOT OVER TASK-WAIT @ AND OVER TASK-WAIT !
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
    2DUP CURRENT-TASK @ SET-WAIT-TIME BEGIN
      PAUSE 2DUP GET-MONOTONIC-TIME SUBTRACT-TIME 0 = SWAP 0 = AND
    UNTIL
    CURRENT-TASK @ UNSET-WAIT-TIME
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
      DUP TASK-WAIT @ WAIT-FD? IF SWAP 1+ SWAP THEN
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
      DUP TASK-WAIT @ WAIT-TIME AND IF DROP TRUE EXIT THEN
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
    DUP TASK-WAIT @ WAIT-TIME AND IF
      ROT ROT 2DROP DUP TASK-WAIT-TIME-S @ OVER TASK-WAIT-TIME-NS @
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
    DUP TASK-WAIT @ WAIT-TIME AND IF
      ROT ROT 2 PICK TASK-WAIT-TIME-S @ 3 PICK TASK-WAIT-TIME-NS @ MIN-TIME
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
      DUP TASK-WAIT @ WAIT-FD? IF
        2DUP TASK-WAIT-FD @ SWAP POLL-FD !
	2DUP TASK-WAIT @ WAIT-IN WAIT-OUT OR WAIT-PRI OR AND SWAP POLL-EVENTS !
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

\ Pause count
VARIABLE PAUSE-COUNT

\ Actually handle pausing
: (PAUSE) ( -- )
  CURRENT-TASK @ IF
    SP@ CURRENT-TASK @ TASK-DATA-STACK !
    RP@ CURRENT-TASK @ TASK-RETURN-STACK !
    HANDLER @ CURRENT-TASK @ TASK-HANDLER !
  THEN
  NEXT-TASK @ IF
    PAUSE-COUNT @ 1 + PAUSE-COUNT !
    NEXT-TASK @ CURRENT-TASK !
    NEXT-TASK @ TASK-NEXT @ NEXT-TASK !
    CURRENT-TASK @ TASK-HANDLER @ HANDLER !    
    GET-TRACE >R FALSE SET-TRACE
    CURRENT-TASK @ TASK-SBASE @ SET-SBASE
    CURRENT-TASK @ TASK-RBASE @ SET-RBASE
    CURRENT-TASK @ TASK-DATA-STACK @ SP!
    R>
    CURRENT-TASK @ TASK-RETURN-STACK @ RP!
    SET-TRACE
    CURRENT-TASK @ TASK-SBASE @ SBASE !
    CURRENT-TASK @ TASK-RBASE @ RBASE !
    CURRENT-TASK @ TASK-ENTRY @ ?DUP IF
      0 CURRENT-TASK @ TASK-ENTRY !
      TRY ?DUP IF
        SINGLE-TASK-IO @ TRUE SINGLE-TASK-IO ! SWAP TRY IF BYE THEN
	SINGLE-TASK-IO !
      THEN
      CURRENT-TASK @ DEACTIVATE-TASK
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

\ Instantiate a task for the main execution; this task uses the default data and
\ return stacks and no entry point, but does have a local data space. This task
\ is activated upon creation.
: NEW-MAIN-TASK ( local-size -- task )
  HERE TASK-HEADER-SIZE ALLOT
  0 OVER TASK-PREV !
  0 OVER TASK-NEXT !
  HERE ROT ALLOT SWAP TUCK TASK-LOCAL !
  SBASE @ OVER TASK-SBASE !
  RBASE @ OVER TASK-RBASE !
  0 OVER TASK-WAIT !
  0 OVER TASK-WAIT-FD !
  0 OVER TASK-WAIT-TIME-S !
  0 OVER TASK-WAIT-TIME-NS !
  0 OVER TASK-HANDLER !
  0 OVER TASK-ACTIVE ! ;

PAUSE-ON-INTERPRET @
FALSE PAUSE-ON-INTERPRET !

1024 CELLS NEW-MAIN-TASK CONSTANT MAIN-TASK
MAIN-TASK ACTIVATE-TASK
MAIN-TASK CURRENT-TASK !

PAUSE

PAUSE-ON-INTERPRET !

\ The task that sleeps the system when no tasks are awake.
1024 1024 256 CELLS ' DO-SLEEP NEW-TASK CONSTANT SLEEP-TASK
SLEEP-TASK ACTIVATE-TASK

\ Abstracting getting the current task
: CURRENT-TASK CURRENT-TASK @ ;

\ Abstracting getting the current number of pauses
: PAUSE-COUNT PAUSE-COUNT @ ;

BASE ! SET-CURRENT SET-ORDER