# SWIM: Scalable Weakly-consistent Infection-style Process Group Membership Protocol

This repo is exercise implementation of [SWIM membership protocol](http://www.cs.cornell.edu/Info/Projects/Spinglass/public_pdfs/SWIM.pdf) based on the course [Cloud Computing](https://coursera.org/learn/cloud-computing). 
The original exercise is in C++. This is same exercise implemented in C#. 

The implementation done in this repo is is efficient SWIM with dissemination done as piggy back on **ping**, **ping-req** and **ack**. 

To see a real implementation of SWIM you can refer to [Serf](https://github.com/hashicorp/serf) repo by Hashicorp which is in golang. 

## TODO:
1. Understand implementation of SWIM in Serf
2. Performance optimizations in this exercise (there is always room for performance improvements)