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

forth-wordlist task-wordlist 2 set-order

100 2 allot-bchan constant my-bchan

: sender
  pause-count 0 begin
    dup my-bchan send-bchan 1 +
    dup $1000 mod 0 = if
      pause-count rot over swap - $1000 swap / ." <" . dup (.) ." > " swap
    then
  again ;

: receiver
  pause-count 0 begin
    my-bchan recv-bchan drop 1 +
    dup $1000 mod 0 = if
      pause-count rot over swap - $1000 swap / ." [" . dup (.) ." ] " swap
    then
  again ;

256 256 512 0 ' sender allot-task constant sender-task
256 256 512 0 ' receiver allot-task constant receiver-task

sender-task activate-task receiver-task activate-task

sleep-task deactivate-task main-task deactivate-task

\ KEY DROP SENDER-TASK DEACTIVATE-TASK RECEIVER-TASK DEACTIVATE-TASK
