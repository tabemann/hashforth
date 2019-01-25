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
#include <stdint.h>
#include "hf/common.h"
#include "hf/inner.h"

/* Initialize hashforth */
void hf_init(hf_global_t* global) {
  hf_cell_t* data_stack_base;
  hf_token_t** return_stack_base;
  global->word_count = 0;
  global->word_space_count = HF_INIT_WORD_SPACE_COUNT;
  if(!(global->words = malloc(sizeof(hf_word_t) * global->word_space_count))) {
    fprintf(stderr, "Unable to allocate words array!\n");
    exit(1);
  }
  global->current_word = NULL;
  global->ip = NULL;
  if(!(data_stack_base =
       malloc(sizeof(hf_cell_t) * HF_INIT_DATA_STACK_COUNT))) {
    fprintf(stderr, "Unable to allocate data stack!\n");
    exit(1);
  }
#ifdef STACK_TRACE
  global->data_stack_base = data_stack_base + HF_INIT_DATA_STACK_COUNT;
  global->old_data_stack_base = global->data_stack_base;
#endif
  global->data_stack = data_stack_base + HF_INIT_DATA_STACK_COUNT;
  if(!(return_stack_base =
       malloc(sizeof(hf_token_t*) * HF_INIT_RETURN_STACK_COUNT))) {
    fprintf(stderr, "Unable to allocate return stack!\n");
    exit(1);
  }
  global->return_stack = return_stack_base + HF_INIT_RETURN_STACK_COUNT;
  if(!(global->std_services = malloc(sizeof(hf_sys_t) * HF_MAX_STD_SERVICES))) {
    fprintf(stderr, "Unable to allocate services!\n");
    exit(1);
  }
  global->std_service_count = 0;
  global->std_service_space_count = HF_MAX_STD_SERVICES;
  for(int i = 0; i < HF_MAX_STD_SERVICES; i++) {
    global->std_services[i].defined = HF_FALSE;
  }
  if(!(global->nstd_services =
       malloc(sizeof(hf_sys_t) * HF_MAX_NSTD_SERVICES))) {
    fprintf(stderr, "Unable to allocate services!\n");
    exit(1);
  }
  global->nstd_service_count = 0;
  global->nstd_service_space_count = HF_MAX_NSTD_SERVICES;
  for(int i = 0; i < HF_MAX_NSTD_SERVICES; i++) {
    global->nstd_services[i].defined = HF_FALSE;
  }
#ifdef TRACE
  global->level = 0;
#endif
  global->trace = HF_FALSE;
}

/* The inner interpreter */
void hf_inner(hf_global_t* global) {
  while(global->ip) {
    hf_full_token_t token = *global->ip++;
#ifdef TOKEN_8_16
    if(token & 0x80) {
      token = (token & 0x7F) | ((hf_full_token_t)(*global->ip++) << 7);
    }
#else
#ifdef TOKEN_16_32
    if(token & 0x8000) {
      token = (token & 0x7FFF) | ((hf_full_token_t)(*global->ip++) << 15);
    }
#endif
#endif
    if(token < global->word_count) {
      hf_word_t* word = global->words + token;
      global->current_word = word;
#ifdef TRACE
      if(global->trace) {
	for(hf_cell_t i = 0; i < global->level; i++) {
	  fprintf(stderr, "  ");
	}
	if(word->name_length) {
	  char* name_copy = malloc(word->name_length);
	  memcpy(name_copy, word->name, word->name_length);
	  name_copy[word->name_length] = 0;
	  fprintf(stderr, "executing token: %lld name: %s data stack: %lld",
		 (uint64_t)token, name_copy, (uint64_t)global->data_stack);
	  free(name_copy);
	} else {
	  fprintf(stderr, "executing token: %lld <no name> data stack: %lld",
		 (uint64_t)token, (uint64_t)global->data_stack);
	}
#ifdef STACK_TRACE
	fprintf(stderr, " [");
	hf_cell_t* stack_trace = global->data_stack;
	hf_cell_t count = 10;
	while(stack_trace < global->data_stack_base && count--) {
	  fprintf(stderr, " %lld", (uint64_t)(*stack_trace++));
	}
	if(stack_trace < global->data_stack_base && !count) {
	  fprintf(stderr, " ...");
	}
	fprintf(stderr, " ]\n");
#else
	fprintf(stderr, "\n");
#endif
      }
#endif
#ifdef STACK_TRACE
#ifdef STACK_CHECK
      if(global->data_stack > global->data_stack_base &&
	 global->data_stack > global->old_data_stack_base) {
	fprintf(stderr, "Data stack underflow!\n");
#ifdef ABORT_ON_END
	abort();
#else
	exit(1);
#endif
      }
#endif
#endif
      word->primitive(global);
    } else {
      fprintf(stderr, "Invalid token!: %d\n", (int)token);
      exit(1);
    }
  }
}

/* Boot the Forth VM */
void hf_boot(hf_global_t* global) {
  if(global->word_count > 0) {
    hf_word_t* word;
    printf("booting token: %lld\n", (uint64_t)(global->word_count - 1));
    word = global->words + global->word_count - 1;
    global->current_word = word;
    word->primitive(global);
    hf_inner(global);
  } else {
    fprintf(stderr, "No tokens registered!\n");
    exit(1);
  }
}

/* Allocate a word */
hf_word_t* hf_new_word(hf_global_t* global, hf_full_token_t token) {
  if(token == global->word_space_count) {
    global->word_space_count *= 2;
    if(!(global->words = realloc(global->words,
				 sizeof(hf_word_t) *
				 global->word_space_count))) {
      fprintf(stderr, "Unable to allocate word space!\n");
      exit(1);
    }
  }
  global->word_count =
    token >= global->word_count ? token + 1 : global->word_count;
  return global->words + token;
}

/* Allocate a service */
hf_sys_t* hf_new_service(hf_global_t* global, hf_sys_index_t index) {
  if(index >= 0) {
    if(index > global->std_service_space_count) {
      if(index < global->std_service_space_count * 2) {
	global->std_service_space_count *= 2;
      } else {
	global->std_service_space_count = index * 2;
      }
      if(!(global->std_services = realloc(global->std_services,
					  sizeof(hf_sys_t) *
					  global->std_service_space_count))) {
	fprintf(stderr, "Unable to allocate service space!\n");
	exit(1);
      }
      for(int i = global->std_service_count;
	  i < global->std_service_space_count; i++) {
	global->std_services[i].defined = HF_FALSE;
      }
    }
    global->std_service_count =
      index >= global->std_service_count ? index + 1 :
      global->std_service_count;
    return global->std_services + index;
  } else {
    index = -index - 1;
    if(index > global->nstd_service_space_count) {
      if(index < global->nstd_service_space_count * 2) {
	global->nstd_service_space_count *= 2;
      } else {
	global->nstd_service_space_count = index * 2;
      }
      if(!(global->nstd_services = realloc(global->nstd_services,
					   sizeof(hf_sys_t) *
					   global->nstd_service_space_count))) {
	fprintf(stderr, "Unable to allocate service space!\n");
	exit(1);
      }
      for(int i = global->nstd_service_count;
	  i < global->nstd_service_space_count; i++) {
	global->nstd_services[i].defined = HF_FALSE;
      }
    }
    global->nstd_service_count =
      index >= global->nstd_service_count ? index + 1 :
      global->nstd_service_count;
    return global->nstd_services + index;
  }
}

/* Allocate a token */
hf_full_token_t hf_new_token(hf_global_t* global) {
  hf_cell_t error = HF_FALSE;
#ifdef TOKEN_8_16
  if(global->word_count == 32768) {
    error = HF_TRUE;
  }
#else
#ifdef TOKEN_16
#ifdef CELL_16
  if(global->word_count == 65535) {
    error = HF_TRUE;
  }
#else
  if(global->word_count == 65536) {
    error = HF_TRUE;
  }
#endif
#else
#ifdef TOKEN_16_32
  if(global->word_count == 0x80000000) {
    error = HF_TRUE;
  }
#else
#ifdef CELL_32 /* This should never be reached under normal circumstances */
  if(global->word_count == 0xFFFFFFFF) {
    error = HF_TRUE;
  }
#endif
#endif
#endif
#endif
  if(error) {
    fprintf(stderr, "Out of available tokens!\n");
    exit(1);
  }
  return global->word_count++;
}
