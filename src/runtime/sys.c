/* Copyright (c) 2018-2019, Travis Bemann
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of the copyright holder nor the names of its
 *    contributors may be used to endorse or promote products derived from
 *    this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE. */

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <strings.h>
#include <stdint.h>
#include <unistd.h>
#include <fcntl.h>
#include <errno.h>
#include <poll.h>
#include <time.h>
#include "hf/common.h"
#include "hf/inner.h"

/* Forward declarations */

/* /\* TYPE service *\/ */
/* void hf_sys_type(hf_global_t* global); */

/* /\* KEY service *\/ */
/* void hf_sys_key(hf_global_t* global); */

/* /\* ACCEPT service *\/ */
/* void hf_sys_accept(hf_global_t* global); */

/* BYE service */
void hf_sys_bye(hf_global_t* global);

/* ALLOCATE service */
void hf_sys_allocate(hf_global_t* global);

/* RESIZE service */
void hf_sys_resize(hf_global_t* global);

/* FREE service */
void hf_sys_free(hf_global_t* global);

/* OPEN service */
void hf_sys_open(hf_global_t* global);

/* CLOSE service */
void hf_sys_close(hf_global_t* global);

/* READ service */
void hf_sys_read(hf_global_t* global);

/* WRITE service */
void hf_sys_write(hf_global_t* global);

/* GET-NONBLOCKING service */
void hf_sys_get_nonblocking(hf_global_t* global);

/* SET-NONBLOCKING service */
void hf_sys_set_nonblocking(hf_global_t* global);

/* ISATTY service */
void hf_sys_isatty(hf_global_t* global);

/* POLL service */
void hf_sys_poll(hf_global_t* global);

/* GET-MONOTONIC-TIME service */
void hf_sys_get_monotonic_time(hf_global_t* global);

/* GET-TRACE service */
void hf_sys_get_trace(hf_global_t* global);

/* SET-TRACE service */
void hf_sys_set_trace(hf_global_t* global);

/* GET-SBASE service */
void hf_sys_get_sbase(hf_global_t* global);

/* SET-SBASE service */
void hf_sys_set_sbase(hf_global_t* global);

/* GET-RBASE service */
void hf_sys_get_rbase(hf_global_t* global);

/* SET-RBASE service */
void hf_sys_set_rbase(hf_global_t* global);

/* Definitions */

/* Register a service */
void hf_register_service(hf_global_t* global, hf_sys_index_t index, char* name,
			 hf_sys_prim_t primitive) {
  hf_cell_t name_length = 0;
  void* name_space = NULL;
  hf_sys_t* service;
  if(name) {
    name_length = strlen(name);
    if(!(name_space = malloc(name_length))) {
      fprintf(stderr, "Unable to allocate name space!\n");
      exit(1);
    }
    memcpy(name_space, name, name_length);
  }
  service = hf_new_service(global, index);
  service->defined = HF_TRUE;
  service->name_length = name_length;
  service->name = name_space;
  service->primitive = primitive;
}

/* Register services */
void hf_register_services(hf_global_t* global) {
  /* hf_register_service(global, HF_SYS_TYPE, "TYPE", hf_sys_type); */
  /* hf_register_service(global, HF_SYS_KEY, "KEY", hf_sys_key); */
  /* hf_register_service(global, HF_SYS_ACCEPT, "ACCEPT", hf_sys_accept); */
  hf_register_service(global, HF_SYS_BYE, "BYE", hf_sys_bye);
  hf_register_service(global, HF_SYS_ALLOCATE, "ALLOCATE", hf_sys_allocate);
  hf_register_service(global, HF_SYS_RESIZE, "RESIZE",  hf_sys_resize);
  hf_register_service(global, HF_SYS_FREE, "FREE", hf_sys_free);
  hf_register_service(global, HF_SYS_OPEN, "OPEN", hf_sys_open);
  hf_register_service(global, HF_SYS_CLOSE, "CLOSE", hf_sys_close);
  hf_register_service(global, HF_SYS_READ, "READ", hf_sys_read);
  hf_register_service(global, HF_SYS_WRITE, "WRITE", hf_sys_write);
  hf_register_service(global, HF_SYS_GET_NONBLOCKING, "GET-NONBLOCKING",
		      hf_sys_get_nonblocking);
  hf_register_service(global, HF_SYS_SET_NONBLOCKING, "SET-NONBLOCKING",
		      hf_sys_set_nonblocking);
  hf_register_service(global, HF_SYS_ISATTY, "ISATTY", hf_sys_isatty);
  hf_register_service(global, HF_SYS_POLL, "POLL", hf_sys_poll);
  hf_register_service(global, HF_SYS_GET_MONOTONIC_TIME, "GET-MONOTONIC-TIME",
		      hf_sys_get_monotonic_time);
  hf_register_service(global, HF_SYS_GET_TRACE, "GET-TRACE", hf_sys_get_trace);
  hf_register_service(global, HF_SYS_SET_TRACE, "SET-TRACE", hf_sys_set_trace);
  hf_register_service(global, HF_SYS_GET_SBASE, "GET-SBASE", hf_sys_get_sbase);
  hf_register_service(global, HF_SYS_SET_SBASE, "SET-SBASE", hf_sys_set_sbase);
  hf_register_service(global, HF_SYS_GET_RBASE, "GET-RBASE", hf_sys_get_rbase);
  hf_register_service(global, HF_SYS_SET_RBASE, "SET-RBASE", hf_sys_set_rbase);
}

/* /\* TYPE service *\/ */
/* void hf_sys_type(hf_global_t* global) { */
/*   hf_cell_t length = *global->data_stack++; */
/*   hf_byte_t* buffer = (hf_byte_t*)(*global->data_stack++); */
/*   fwrite(buffer, sizeof(hf_byte_t), length, stderr); */
/* } */

/* /\* KEY service *\/ */
/* void hf_sys_key(hf_global_t* global) { */
/*   int key = fgetc(stdin); */
/*   if(key != EOF) { */
/*     *(--global->data_stack) = key; */
/*   } else { */
/*     exit(0); */
/*   } */
/* } */

/* /\* ACCEPT service *\/ */
/* void hf_sys_accept(hf_global_t* global) { */
/*   hf_cell_t buffer_size = *global->data_stack++; */
/*   hf_byte_t* buffer = (hf_byte_t*)(*global->data_stack); */
/*   hf_byte_t* data_read = fgets(buffer, buffer_size, stdin); */
/*   if(data_read) { */
/*     hf_cell_t data_read_length = strlen(data_read); */
/*     if(data_read[data_read_length - 1] == '\n') { */
/*       data_read_length--; */
/*     } */
/*     *global->data_stack = data_read_length; */
/*   } else { */
/*     *global->data_stack = 0; */
/*   } */
/* } */

/* BYE service */
void hf_sys_bye(hf_global_t* global) {
  exit(0);
}

/* ALLOCATE service */
void hf_sys_allocate(hf_global_t* global) {
  size_t size = *global->data_stack++;
  *(--global->data_stack) = (hf_cell_t)malloc(size);
  *(--global->data_stack) = HF_TRUE;
}

/* RESIZE service */
void hf_sys_resize(hf_global_t* global) {
  size_t size = *global->data_stack++;
  void* buffer = (void*)(*global->data_stack++);
  *(--global->data_stack) = (hf_cell_t)realloc(buffer, size);
  *(--global->data_stack) = HF_TRUE;
}

/* FREE service */
void hf_sys_free(hf_global_t* global) {
  void* buffer = (void*)(*global->data_stack++);
  free(buffer);
  *(--global->data_stack) = HF_TRUE;
}

/* OPEN service */
void hf_sys_open(hf_global_t* global) {
  int mode = *global->data_stack++;
  hf_cell_t flags = *global->data_stack++;
  int flags_conv = 0;
  hf_cell_t name_length = *global->data_stack++;
  hf_byte_t* name = (hf_byte_t*)(*global->data_stack++);
  char* name_copy;
  int fd;
  hf_cell_t done = HF_FALSE;
  if(!(name_copy = malloc(name_length + 1))) {
    fprintf(stderr, "Unable to allocate memory!\n");
    exit(1);
  }
  memcpy(name_copy, name, name_length);
  name_copy[name_length] = 0;
  if(flags & HF_OPEN_RDONLY) {
    flags_conv |= O_RDONLY;
  }
  if(flags & HF_OPEN_WRONLY) {
    flags_conv |= O_WRONLY;
  }
  if(flags & HF_OPEN_RDWR) {
    flags_conv |= O_RDWR;
  }
  if(flags & HF_OPEN_APPEND) {
    flags_conv |= O_APPEND;
  }
  if(flags & HF_OPEN_CREAT) {
    flags_conv |= O_CREAT;
  }
  if(flags & HF_OPEN_EXCL) {
    flags_conv |= O_EXCL;
  }
  if(flags & HF_OPEN_TRUNC) {
    flags_conv |= O_TRUNC;
  }
  while(!done) {
    if((fd = open(name_copy, flags, mode)) != -1) {
      *(--global->data_stack) = fd;
      *(--global->data_stack) = HF_TRUE;
      done = HF_TRUE;
    } else if(errno != EINTR) {
      *(--global->data_stack) = 0;
      *(--global->data_stack) = HF_FALSE;
      done = HF_TRUE;
    }
  }
  free(name_copy);
}

/* CLOSE service */
void hf_sys_close(hf_global_t* global) {
  int fd = *global->data_stack++;
  hf_cell_t done = HF_FALSE;
  while(!done) {
    if(!close(fd)) {
      *(--global->data_stack) = HF_TRUE;
      done = HF_TRUE;
    } else if(errno != EINTR) {
      *(--global->data_stack) = HF_FALSE;
      done = HF_TRUE;
    }
  }
}

/* READ service */
void hf_sys_read(hf_global_t* global) {
  int fd = *global->data_stack++;
  size_t count = *global->data_stack++;
  void* buffer = (void*)(*global->data_stack++);
  hf_cell_t done = HF_FALSE;
  ssize_t read_count;
  while(!done) {
    if((read_count = read(fd, buffer, count)) != -1) {
      *(--global->data_stack) = read_count;
      *(--global->data_stack) = HF_TRUE;
      done = HF_TRUE;
    } else if(errno != EINTR) {
      *(--global->data_stack) = 0;
      if(errno == EAGAIN || errno == EWOULDBLOCK) {
	*(--global->data_stack) = HF_WOULDBLOCK;
      } else {
	*(--global->data_stack) = HF_FALSE;
      }
      done = HF_TRUE;
    }
  }
}

/* WRITE service */
void hf_sys_write(hf_global_t* global) {
  int fd = *global->data_stack++;
  size_t count = *global->data_stack++;
  void* buffer = (void*)(*global->data_stack++);
  hf_cell_t done = HF_FALSE;
  ssize_t write_count;
  while(!done) {
    if((write_count = write(fd, buffer, count)) != -1) {
      *(--global->data_stack) = write_count;
      *(--global->data_stack) = HF_TRUE;
      done = HF_TRUE;
    } else if(errno != EINTR) {
      *(--global->data_stack) = 0;
      if(errno == EAGAIN || errno == EWOULDBLOCK) {
	*(--global->data_stack) = HF_WOULDBLOCK;
      } else {
	*(--global->data_stack) = HF_FALSE;
      }
      done = HF_TRUE;
    }
  }
}

/* GET-NONBLOCKING service */
void hf_sys_get_nonblocking(hf_global_t* global) {
  int fd = *global->data_stack++;
  int flags;
  hf_cell_t done = HF_FALSE;
  while(!done) {
    if((flags = fcntl(fd, F_GETFL, 0)) != -1) {
      *(--global->data_stack) = flags & O_NONBLOCK ? HF_TRUE : HF_FALSE;
      *(--global->data_stack) = HF_TRUE;
      done = HF_TRUE;
    } else if(errno != EINTR) {
      *(--global->data_stack) = HF_FALSE;
      *(--global->data_stack) = HF_FALSE;
      done = HF_TRUE;
    }
  }
}

/* SET-NONBLOCKING service */
void hf_sys_set_nonblocking(hf_global_t* global) {
  int fd = *global->data_stack++;
  hf_cell_t nonblocking = *global->data_stack++;
  int flags;
  hf_cell_t done = HF_FALSE;
  while(!done) {
    if((flags = fcntl(fd, F_GETFL, 0)) != -1) {
      if(nonblocking) {
	flags |= O_NONBLOCK;
      } else {
	flags &= ~O_NONBLOCK;
      }
      while(!done) {
	if(fcntl(fd, F_SETFL, flags) != -1) {
	  *(--global->data_stack) = HF_TRUE;
	  done = HF_TRUE;
	} else if(errno != EINTR) {
	  *(--global->data_stack) = HF_FALSE;
	  done = HF_TRUE;
	}
      }
    } else if(errno != EINTR) {
      *(--global->data_stack) = HF_FALSE;
      done = HF_TRUE;
    }
  }
}

/* ISATTY service */
void hf_sys_isatty(hf_global_t* global) {
  int fd = *global->data_stack++;
  if(isatty(fd) == 1) {
    *(--global->data_stack) = HF_TRUE;
    *(--global->data_stack) = HF_TRUE;
  } else if(errno == EINVAL) {
    *(--global->data_stack) = HF_FALSE;
    *(--global->data_stack) = HF_TRUE;
  } else {
    *(--global->data_stack) = HF_FALSE;
    *(--global->data_stack) = HF_FALSE;
  }
}

/* POLL service */
void hf_sys_poll(hf_global_t* global) {
  int timeout = *global->data_stack++;
  nfds_t nfds = *global->data_stack++;
  hf_cell_t* fd_info = (hf_cell_t*)(*global->data_stack++);
  struct pollfd* fds = malloc(sizeof(struct pollfd) * nfds);
  int count;
  for(hf_cell_t i = 0; i < nfds; i++) {
    hf_cell_t events = *(fd_info + (i * 3) + 1);
    fds[i].fd = *(fd_info + (i * 3));
    fds[i].events = 0;
    fds[i].revents = 0;
    if(events & HF_POLL_IN) {
      fds[i].events |= POLLIN;
    }
    if(events & HF_POLL_OUT) {
      fds[i].events |= POLLOUT;
    }
    if(events & HF_POLL_PRI) {
      fds[i].events |= POLLPRI;
    }
  }
  if((count = poll(fds, nfds, timeout)) != -1) {
    for(hf_cell_t i = 0; i < nfds; i++) {
      hf_cell_t revents = 0;
      fds[i].fd = *(fd_info + (i * 3));
      fds[i].events = 0;
      fds[i].revents = 0;
      if(fds[i].revents & POLLIN) {
	revents |= HF_POLL_IN;
      }
      if(fds[i].revents & POLLOUT) {
	revents |= HF_POLL_OUT;
      }
      if(fds[i].revents & POLLPRI) {
	revents |= HF_POLL_PRI;
      }
      if(fds[i].revents & POLLERR) {
	revents |= HF_POLL_ERR;
      }
      if(fds[i].revents & POLLHUP) {
	revents |= HF_POLL_HUP;
      }
      if(fds[i].revents & POLLNVAL) {
	revents |= HF_POLL_NVAL;
      }
      *(fd_info + (i * 3) + 2) = revents;
    }
  } else if(errno == EINTR) {
    *(--global->data_stack) = 0;
    *(--global->data_stack) = HF_TRUE;
  } else {
    *(--global->data_stack) = 0;
    *(--global->data_stack) = HF_FALSE;
  }
  free(fds);
}

/* GET-MONOTONIC-TIME service */
void hf_sys_get_monotonic_time(hf_global_t* global) {
  struct timespec monotonic_time;
  clock_gettime(CLOCK_MONOTONIC, &monotonic_time);
  *(--global->data_stack) = monotonic_time.tv_sec;
  *(--global->data_stack) = monotonic_time.tv_nsec;
}

/* GET-TRACE service */
void hf_sys_get_trace(hf_global_t* global) {
  *(--global->data_stack) = global->trace;
}

/* SET-TRACE service */
void hf_sys_set_trace(hf_global_t* global) {
  global->trace = *global->data_stack++;
}

/* GET-SBASE service */
void hf_sys_get_sbase(hf_global_t* global) {
#ifdef STACK_TRACE
  *(--global->data_stack) = (hf_cell_t)global->data_stack_base;
#else
  *(--global->data_stack) = 0;
#endif
}

/* SET-SBASE service */
void hf_sys_set_sbase(hf_global_t* global) {
#ifdef STACK_TRACE
  global->old_data_stack_base = global->data_stack_base;
  global->data_stack_base = (hf_cell_t*)(*global->data_stack++);
#endif
}

/* GET-RBASE service */
void hf_sys_get_rbase(hf_global_t* global) {
#ifdef TRACE
  *(--global->data_stack) = (hf_cell_t)global->return_stack_base;
#else
  *(--global->data_stack) = 0;
#endif
}

/* SET-RBASE service */
void hf_sys_set_rbase(hf_global_t* global) {
#ifdef TRACE
  global->return_stack_base = (hf_token_t**)(*global->data_stack++);
#endif
}
