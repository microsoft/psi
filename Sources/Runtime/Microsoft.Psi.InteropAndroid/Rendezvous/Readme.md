# Interop Rendezvous

A distributed \psi system may have many separate pipelines running in separate processes, on separate machines, publishing and subscribing to streams being conveyed using various protocols. The rendezvous system allows each pipeline process to advertise its available streams and to discover those of other pipelines. This is accomplished by a centralized "rendezvous point" which maintains and relays endpoint connection and stream information.

For more information, see [the Rendezvous System wiki page](https://github.com/microsoft/psi/wiki/Rendezvous-System).
