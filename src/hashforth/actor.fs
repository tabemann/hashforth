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

forth-wordlist intmap-wordlist vector-wordlist lambda-wordlist task-wordlist
5 set-order
task-wordlist set-current

\ Initial actor map size
16 constant initial-actor-map-count

\ Actor map
1 cells initial-actor-map-count allocate-intmap constant actor-map

\ Initial task to actor map size
16 constant inital-task-actor-map-count

\ Task to actor map
1 cells inital-task-actor-map-count allocate-intmap constant task-actor-map

\ Initial actors awaiting destruction channel count
16 constant initial-dead-actor-count

\ Actors awaiting destruction channel
initial-dead-actor-count 1 cells 1 allocate-bufchan constant dead-actor-chan

\ Initial actor termination notification map size
4 constant initial-actor-end-notify-count

\ Actor user variable
user current-actor-obj

\ Actor init xt
user actor-xt

\ Actor init data size
user actor-data-size

\ Actor init data
user actor-data

\ Actor started flag
1 constant actor-started

\ Actor structure
begin-structure actor-size
  \ Actor index
  field: actor-index
  
  \ Actor task
  field: actor-task

  \ Actor message channel
  field: actor-chan

  \ Actor mutex
  field: actor-mutex

  \ Actor termination notification map
  field: actor-end-notify-map

  \ Actor flags
  field: actor-flags
end-structure

\ Actor configuration structure
begin-structure actor-config-size
  \ Actor data stack size
  field: actor-data-stack-size

  \ Actor return stack size
  field: actor-return-stack-size

  \ Actor user space size
  field: actor-user-size

  \ Actor locals count (0 means default to #USER)
  field: actor-locals-count

  \ Actor message channel default queue count
  field: actor-chan-queue-count

  \ Actor message channel default block size
  field: actor-chan-block-size

  \ Actor mutex waiting count
  field: actor-mutex-count
end-structure

\ Actor termination message
begin-structure actor-end-size
  \ Actor termination message magic
  field: actor-end-magic

  \ Actor termination index
  field: actor-end-index
end-structure

\ Actor termination message magic constant
$DEADBEEF constant actor-end-magic-const
  
\ Next actor index
variable next-actor-index 0 next-actor-index !

\ Default actor configuration
here actor-config-size allot
64 over actor-data-stack-size !
64 over actor-return-stack-size !
1024 over actor-user-size !
0 over actor-locals-count !
64 over actor-chan-queue-count !
64 over actor-chan-block-size !
16 over actor-mutex-count !
constant default-actor-config

\ Nonexistant actor value
-1 constant nonexist-actor

\ Current task is not an actor exception
: x-current-task-not-actor ( -- ) space ." current task is not an actor" cr ;

\ Send a message to an actor
: send-actor ( addr bytes actor-index -- )
  actor-map get-intmap-cell if
    dup actor-mutex @ lock-mutex
    rot rot 2 pick actor-chan @ send-varchan
    actor-mutex @ unlock-mutex
  else
    drop
  then ;

\ Notify subscribers for actor termination
: actor-notify-end ( actor -- )
  [:
    nip here actor-end-size allot
    actor-end-magic-const over actor-end-magic !
    2 pick actor-index @ over actor-end-index !
    actor-end-size rot send-actor
    actor-end-size negate allot
  ;] over actor-end-notify-map @ iter-intmap drop ;

\ Kill current task
: kill-current-actor ( -- )
  current-actor-obj @ actor-index @ actor-map delete-intmap drop
  current-actor-obj @ task-actor-map delete-intmap drop
  current-actor-obj @ here ! here 1 cells allot dead-actor-chan send-bufchan
  -1 cells allot
  current-actor-obj @ actor-task @ deactivate-task ;

\ Run an actor
: run-actor ( -- ) actor-xt @ try ?dup if execute then kill-current-actor ;

\ Task to destroy dead tasks
: run-reaper ( -- )
  begin
    here 1 cells allot dup dead-actor-chan recv-bufchan -1 cells allot
    here @
    dup actor-mutex @ lock-mutex
    dup actor-mutex @ unlock-mutex
    dup actor-notify-end
    dup actor-task @ destroy-task
    dup actor-chan @ destroy-varchan
    dup actor-end-notify-map @ destroy-intmap
    dup actor-mutex @ destroy-mutex
    free!
  again ;

\ Create the reaper
64 64 512 0 ' run-reaper allocate-task constant reaper

\ Activate the reaper
reaper activate-task

\ Spawn an actor with data
: spawn-actor-with-data ( config addr bytes xt -- actor-index )
  3 roll >r actor-size allocate!
  r@ actor-data-stack-size @
  r@ actor-return-stack-size @
  r@ actor-user-size @
  r@ actor-locals-count @
  ['] run-actor allocate-task over actor-task !
  ['] task-here over actor-task @ access-task @
  ['] actor-data 2 pick actor-task @ access-task !
  swap ['] actor-xt 2 pick actor-task @ access-task !
  over ['] actor-data-size 2 pick actor-task @ access-task !
  ['] task-here over actor-task @ access-task @
  swap >r swap move r>
  ['] actor-data-size over actor-task @ access-task @
  ['] task-here 2 pick actor-task @ access-task +!
  dup ['] current-actor-obj 2 pick actor-task @ access-task !
  r@ actor-chan-queue-count @
  r@ actor-chan-block-size  @
  1 1 1 allocate-varchan over actor-chan !
  r> actor-mutex-count @ allocate-mutex over actor-mutex !
  1 cells initial-actor-end-notify-count allocate-intmap
  over actor-end-notify-map !
  next-actor-index @ over actor-index !
  1 next-actor-index +!
  dup dup actor-index @ actor-map set-intmap-cell drop
  dup dup actor-index @ swap actor-task @ task-actor-map set-intmap-cell drop
  0 over actor-flags !
  actor-index @ ;

\ Spawn an actor without data
: spawn-actor ( config xt -- actor-index ) here 0 rot spawn-actor-with-data ;

\ Start an actor
: start-actor ( actor-index -- )
  actor-map get-intmap-cell if
    dup actor-flags @ actor-started and 0 = if
      dup actor-flags @ actor-started or over actor-flags !
      actor-task @ activate-task
    else
      drop
    then
  else
    drop
  then ;
  
\ Get whether the current task is an actor
: current-task-actor? ( -- actor? )
  current-task task-actor-map member-intmap ;

\ Get the current actor
: current-actor ( -- actor-index )
  current-task-actor? if
    current-actor-obj @ actor-index @
  else
    nonexist-actor
  then ;

\ Kill an actor
: kill-actor ( actor-index -- )
  dup current-actor <> if
    dup actor-map get-intmap-cell if
      swap actor-map delete-intmap drop
      dup task-actor-map delete-intmap drop
      dup actor-mutex @ lock-mutex
      dup actor-mutex @ unlock-mutex
      dup actor-notify-end
      dup actor-task @ destroy-task
      dup actor-chan @ destroy-varchan
      dup actor-end-notify-map @ destroy-intmap
      dup actor-mutex @ destroy-mutex
      free!
    else
      2drop
    then
  else
    drop kill-current-actor
  then ;

\ Receive for the current actor - note that it allocates the data received
: recv-actor ( -- addr bytes )
  current-task-actor? averts x-current-task-not-actor
  current-actor-obj @ actor-chan @ allocate-recv-varchan ;

\ Get whether an actor exists
: actor-exists? ( actor-index ) actor-map member-intmap ;

\ Get whether message is an actor termination message, and if so, get which
\ actor terminated
: actor-check-end ( addr bytes -- actor-index is-end )
  actor-end-size = if
    dup actor-end-magic @ actor-end-magic-const = if
      actor-end-index @ true
    else
      drop 0 false
    then
  else
    drop 0 false
  then ;

\ Subscribe to actor termination notification
: subscribe-actor-end ( subscriber-actor-index publisher-actor-index -- )
  0 rot rot actor-map get-intmap-cell if
    actor-end-notify-map @ set-intmap-cell drop
  else
    2drop drop
  then ;

\ Unsubscribe to actor termination notification
: unsubscribe-actor-end ( subscriber-actor-index publisher-actor-index -- )
  actor-map get-intmap-cell if
    actor-end-notify-map @ delete-intmap drop
  else
    2drop
  then ;

base ! set-current set-order
