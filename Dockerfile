FROM node:14-alpine

WORKDIR /app

COPY vowel_counter.js .

ENTRYPOINT ["node", "vowel_counter.js"]

