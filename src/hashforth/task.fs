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

get-order get-current base @

decimal
forth-wordlist 1 set-order
forth-wordlist set-current

wordlist constant task-wordlist

forth-wordlist task-wordlist 2 set-order

forth-wordlist set-current

\ Allocate a user variable
: user ( "name" -- ) create #user @ cells , 1 #user +! does> @ up @ + ;

task-wordlist set-current

0 constant no-wait
1 constant wait-in
2 constant wait-out
4 constant wait-pri
8 constant wait-time

variable first-task
variable next-task
variable current-task
variable awake-task-count

\ Current data stack pointer
user task-data-stack

\ Current return stack pointer
user task-return-stack

\ Current user space pointer
user task-here

\ Current entry xt
user task-entry

\ Current active value (< 1 inactive, >= 1 active)
user task-active

\ Data stack base
user task-sbase

\ Return stack base
user task-rbase

\ Task wait flags
user task-wait

\ Task wait file descriptor
user task-wait-fd

\ Task wait time seconds
user task-wait-time-s

\ Task wait time nanoseconds
user task-wait-time-ns

begin-structure task-header-size
  field: task-prev
  field: task-next
  field: task-up
end-structure

begin-structure poll-fd-size
  field: poll-fd
  field: poll-events
  field: poll-revents
end-structure

\ Access a task's user variables
: access-task ( xt task -- ) up @ >r task-up @ up ! execute r> up ! ;

\ Instantiate a new task allocated in the user space with the given data stack
\ and return stack sizes in cells, the given user space size in bytes, the
\ given local space size in cells, and the given entry point.
: new-task
  ( data-stack-size return-stack-size user-size local-size entry -- task )
  here task-header-size allot
  0 over task-prev !
  0 over task-next !
  here 4 roll dup #user @ cells < if
    drop #user @ cells
  then
  allot over task-up !
  dup task-up @ 3 roll dup #user @ < if
    drop #user @
  then
  2dup cells 0 fill \ Fill all the user variables with zeros
  cells + ['] task-here 2 pick access-task !
  tuck ['] task-entry swap access-task !
  rot cells allot here ['] task-data-stack 2 pick access-task !
  swap cells allot here ['] task-return-stack 2 pick access-task !
  ['] task-return-stack over access-task @ ['] task-rbase 2 pick access-task !
  ['] task-data-stack over access-task @ ['] task-sbase 2 pick access-task !
  0 ['] task-active 2 pick access-task !
  0 ['] handler 2 pick access-task !
  10 ['] base 2 pick access-task !
  0 ['] task-wait 2 pick access-task !
  0 ['] task-wait-fd 2 pick access-task !
  0 ['] task-wait-time-s 2 pick access-task !
  0 ['] task-wait-time-ns 2 pick access-task !
  stdin ['] input-fd 2 pick access-task !
  stdout ['] output-fd 2 pick access-task !
  stderr ['] error-fd 2 pick access-task !
  0 ['] read-key 2 pick access-task !
  false ['] read-key? 2 pick access-task !
  here format-digit-count allot
  ['] format-digit-buffer 2 pick access-task ! ;

\ Not in a task!
: x-not-in-task space ." not in a task" cr ;

\ Get whether a task is currently running.
: in-task? ( -- flag ) current-task @ ;

\ Internal word for actually activating a task.
: (activate-task) ( task -- )
  first-task @ if
    first-task @ over task-next !
    first-task @ task-prev @ over task-prev !
    dup first-task @ task-prev @ task-next !
    dup first-task @ task-prev !
    next-task @ first-task @ = if
      dup next-task !
    then
  else
    dup dup task-next !
    dup dup task-prev !
    dup next-task !
  then
  dup first-task !
  ['] task-wait swap access-task @ no-wait = if
    awake-task-count @ 1+ awake-task-count !
  then ;

\ Activate a task for execution.
: activate-task ( task -- )
  ['] task-active over access-task @ 1 + ['] task-active 2 pick access-task !
  ['] task-active over access-task @ 1 = if (activate-task) else drop then ;

\ Force the activation of a task.
: force-activate-task ( task -- )
  ['] task-active over access-task @ 1 < if
    1 ['] task-active 2 pick access-task ! (activate-task)
  else
    drop
  then ;

\ Last task deactivated exception
: x-last-task-deactivated ( -- ) space ." last task deactivated" cr ;

\ Deactivate the last task.
: deactivate-last-task ( task -- )
  current-task @ = if sp@ task-data-stack ! rp@ task-return-stack ! then
  0 first-task !
  0 next-task !
  0 current-task !
  0 awake-task-count !
  ['] x-last-task-deactivated ?raise ;

\ Internal word for actually deactivating tasks.
: (deactivate-task) ( task -- )
  dup task-next @ over <> if
    ['] task-wait over access-task @ no-wait = if
      awake-task-count @ 1- awake-task-count !
    then
    dup next-task @ = if
      dup task-next @ next-task !
    then
    dup task-prev @ over task-next @ task-prev !
    dup task-next @ over task-prev @ task-next !
    dup first-task @ = if
      dup task-next @ first-task !
    then
    current-task @ = if
      sp@ task-data-stack ! rp@ task-return-stack ! here task-here !
      0 current-task !
      pause
    then
  else
    deactivate-last-task
  then ;

\ Deactivate a task (remove it from execution).
: deactivate-task ( task -- )
  ['] task-active over access-task @ 1 - ['] task-active 2 pick access-task !
  ['] task-active over access-task @ 0 = if (deactivate-task) else drop then ;

\ Force the deactivation of a task.
: force-deactivate-task ( task -- )
  ['] task-active over access-task @ 0 > if
    0 ['] task-active 2 pick access-task ! (deactivate-task)
  else
    drop
  then ;

\ Services for multitasking
variable sys-poll
variable sys-get-monotonic-time
variable sys-set-sbase
variable sys-set-rbase

\ Poll on one or more file descriptors (a return value of -1 means success and
\ a return value of 0 means error).
: poll ( fds nfds timeout-ms -- fds-ready -1|0 ) sys-poll @ sys ;

\ Get monotonic time
: get-monotonic-time ( -- s ns ) sys-get-monotonic-time @ sys ;

\ Set a system-wide SBASE for tracing purposes
: set-sbase ( sbase -- ) sys-set-sbase @ sys ;

\ Set a system-wide RBASE for tracing purposes
: set-rbase ( rbase -- ) sys-set-rbase @ sys ;

\ Set a task as waiting on reading a file descriptor.
: set-wait-in ( fd -- )
  task-wait @ 0 = if awake-task-count @ 1 - awake-task-count ! then
  task-wait-fd ! task-wait @ wait-in or task-wait ! ;

\ Set a task as not waiting on reading a file descriptor.
: unset-wait-in ( -- )
  0 task-wait-fd ! wait-in not task-wait @ and task-wait !
  task-wait @ 0 = if awake-task-count @ 1 + awake-task-count ! then ;

\ Set a task as waiting on writing a file descriptor.
: set-wait-out ( fd -- )
  task-wait @ 0 = if awake-task-count @ 1 - awake-task-count ! then
  task-wait-fd ! task-wait @ wait-out or task-wait ! ;

\ Set a task as not waiting on writing a file descriptor.
: unset-wait-out ( -- )
  0 task-wait-fd ! wait-out not task-wait @ and task-wait !
  task-wait @ 0 = if awake-task-count @ 1 + awake-task-count ! then ;

\ Set a task as waiting on reading priority data from a file descriptor.
: set-wait-pri ( fd -- )
  task-wait @ 0 = if awake-task-count @ 1 - awake-task-count ! then
  task-wait-fd ! task-wait @ wait-pri or task-wait ! ;

\ Set a task as not waiting on reading priority data from a file descriptor.
: unset-wait-pri ( -- )
  0 task-wait-fd ! wait-pri not task-wait @ and task-wait !
  task-wait @ 0 = if awake-task-count @ 1 + awake-task-count ! then ;

\ Set a task as waiting for a given time.
: set-wait-time ( s ns -- )
  task-wait @ 0 = if awake-task-count @ 1 - awake-task-count ! then
  task-wait-time-ns ! task-wait-time-s !
  task-wait @ wait-time or task-wait ! ;

\ Set a task as not waiting on reading priority data from a file descriptor.
: unset-wait-time ( -- )
  0 task-wait-time-ns ! 0 task-wait-time-s !
  wait-time not task-wait @ and task-wait !
  task-wait @ 0 = if awake-task-count @ 1 + awake-task-count ! then ;

\ Subtract one time from another.
: subtract-time ( s1 ns1 s2 ns2 -- s3 ns3 )
  3 pick 2 pick > if
    2 pick over >= if
      3 roll rot - rot rot -
    else
      3 roll rot - 1- rot 1000000000 + rot -
    then
  else 3 pick 2 pick = 3 pick 2 pick > and if
    3 roll rot 2drop - 0 swap
  else 2drop 2drop 0 0 then then ;

\ Sleep until a set time.
: sleep-until ( s ns -- )
  2dup get-monotonic-time subtract-time 0 <> swap 0 <> or if
    2dup set-wait-time begin
      pause 2dup get-monotonic-time subtract-time 0 = swap 0 = and
    until
    unset-wait-time
  then
  2drop ;

\ Sleep a given amount time.
: sleep ( s ns -- ) get-monotonic-time time+ sleep-until ;

\ Get whether a wait is for POLLIN, POLLOUT, or POLLPRI.
: wait-fd? ( wait -- flag )
  dup wait-in and over wait-out and or swap wait-pri and or ;

\ Get number of tasks that need to wait on file descriptors.
: get-fd-wait-count ( -- )
  first-task @ 0 <> if
    0 first-task @ begin
      ['] task-wait over access-task @ wait-fd? if swap 1+ swap then
      task-next @
    dup first-task @ = until
    drop
  else
    0
  then ;

\ Get whether polling should sleep.
: sleep? ( -- flag)
  first-task @ 0 <> if
    first-task @ begin
      ['] task-wait over access-task @ wait-time and if drop true exit then
      task-next @
    dup first-task @ = until
    drop false
  else
    false
  then ;

\ Get the first time available for sleeping and the task with that time, or
\ 0 s 0 ns if no first time is available.
: get-first-sleep-time ( -- s ns task | 0 0 first-task )
  0 0 first-task @ begin
    ['] task-wait over access-task @ wait-time and if
      rot rot 2drop
      ['] task-wait-time-s over access-task @
      ['] task-wait-time-ns 2 pick access-task @
      rot task-next @ true
    else
      task-next @ dup first-task @ =
    then
  until ;

\ Get the minimum of two times.
: min-time ( s1 ns1 s2 ns2 -- s3 ns3 )
  3 pick 2 pick = if
    2 pick over <= if 2drop
    else rot drop rot drop then
  else 3 pick 2 pick < if 2drop
  else rot drop rot drop then then ;

\ Find the earliest sleep time.
: find-earliest-sleep-time ( s1 ns1 task -- s2 ns2 )
  begin
    ['] task-wait over access-task @ wait-time and if
      rot rot
      ['] task-wait-time-s 3 pick access-task @
      ['] task-wait-time-ns 4 pick access-task @ min-time
      rot
    then
  task-next @ dup first-task @ = until
  drop ;

\ Convert a time into milliseconds.
: convert-time-to-ms ( s ns -- ms ) 1000000 / swap 1000 * + ;

\ Get the sleep time for polling.
: get-sleep-time ( -- )
  sleep? if
    get-first-sleep-time
    dup first-task @ <> if find-earliest-sleep-time else drop then
    get-monotonic-time subtract-time convert-time-to-ms
  else
    -1
  then ;

\ Actually populate the polling file descriptors.
: populate-poll-fds ( poll-fds -- )
  first-task @ 0 <> if
    first-task @ begin
      ['] task-wait over access-task @ wait-fd? if
        2dup ['] task-wait-fd swap access-task @ swap poll-fd !
	2dup ['] task-wait swap access-task @
	wait-in wait-out or wait-pri or and swap poll-events !
	over poll-revents 0 swap !
	swap poll-fd-size + swap
      then
      task-next @
    dup first-task @ = until
    2drop
  else
    drop
  then ;

\ The function that sleeps the system when no tasks are awake.
: do-sleep ( -- )
  begin
    awake-task-count @ 1 = if
      get-fd-wait-count here over poll-fd-size * allot
      dup populate-poll-fds over get-sleep-time poll poll-fd-size * negate allot
    then
    pause
  again ;

\ Function to call on pause
variable 'on-pause

\ Pause count
variable pause-count

\ Actually handle pausing
: (pause) ( -- )
  current-task @ if
    sp@ task-data-stack !
    rp@ task-return-stack !
    here task-here !
  then
  next-task @ if
    'on-pause @ ?dup if execute then
    pause-count @ 1 + pause-count !
    next-task @ current-task !
    next-task @ task-next @ next-task !
    current-task @ task-up @ up !
    task-here @ here!
    get-trace >r false set-trace
    task-sbase @ set-sbase
    task-rbase @ set-rbase
    task-data-stack @ sp!
    r>
    task-return-stack @ rp!
    set-trace
    task-sbase @ sbase !
    task-rbase @ rbase !
    task-entry @ ?dup if
      0 task-entry !
      try ?dup if
        single-task-io @ true single-task-io ! swap try if bye then
	single-task-io !
      then
      current-task @ force-deactivate-task
    then
  then ;

\ Initialize multitasking
: init-tasks ( -- )
  s" POLL" sys-lookup sys-poll !
  s" GET-MONOTONIC-TIME" sys-lookup sys-get-monotonic-time !
  s" SET-SBASE" sys-lookup sys-set-sbase !
  s" SET-RBASE" sys-lookup sys-set-rbase !
  ['] (pause) 'pause ! ;

init-tasks

\ Instantiate a task for the main execution.
: new-main-task ( -- task )
  here task-header-size allot
  0 over task-prev !
  0 over task-next !
  up @ over task-up !
  here ['] task-here 2 pick access-task !
  sbase @ ['] task-sbase 2 pick access-task !
  rbase @ ['] task-rbase 2 pick access-task !
  0 ['] task-active 2 pick access-task !
  0 ['] task-entry 2 pick access-task !
  0 ['] task-wait 2 pick access-task !
  0 ['] task-wait-fd 2 pick access-task !
  0 ['] task-wait-time-s 2 pick access-task !
  0 ['] task-wait-time-ns 2 pick access-task !
  dup activate-task dup current-task ! pause ;

new-main-task constant main-task

\ The task that sleeps the system when no tasks are awake.
1024 1024 1024 cells 0 ' do-sleep new-task constant sleep-task
sleep-task activate-task

\ Abstracting getting the current task
: current-task current-task @ ;

\ Abstracting getting the first task
: first-task first-task @ ;

\ Abstracting getting the next task
: next-task next-task @ ;

\ Abstracting getting the current number of pauses
: pause-count pause-count @ ;

base ! set-current set-order
