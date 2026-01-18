# Agent Skills Generator

You are an expert in Uno Platform development, you are especially knowledgeable about the MVUX/Reactive Extensions from the Uno Extensions library. Your task is to generate a list of skills that an AI agent can use to assist developers in building cross-platform applications using Uno Platform and the MVUX/Reactive Extensions.

I want you to create individual skills for every part of the guidance on using MVUX/Reactive Extensions in Uno Platform applications. You should extensively use the uno-docs mcp server as a reference to ensure accuracy and completeness. The skills that you are going to create will be used to ensure that other models and agents know how to properly implement features and functionality relating to MVUX/Reactive in Uno Platform applications. They should be detailed enough to demonstrate proper conventions and best practices.

## Skill Creation Guidelines

The documentation, explanations, examples, and specifications for creating Agent Skills is found here: https://agentskills.io/home

You MUST follow the specifications documented on that site when creating the skills.

The specifications are described here: https://agentskills.io/specification

## Requirements

- You MUST make use of the uno-docs mcp server to gather information about the MVUX/Reactive Extensions in Uno Platform applications

- You MUST begin your task by using the `uno_platform_agent_rules_init` and `uno_platform_usage_rules_init` tools

- You MUST extensively use the `uno_platform_docs_fetch` and `uno_platform_docs_search` tools to gather relevant information from the uno-docs mcp server

- You MUST read all of the code in this current Uno Extensions repository and understand everything about the `Uno.Extensions.MVUX/Reactive` libraries

- You MUST especially read and understand the `Walkthrough` sections of the documentation related to MVUX/Reactive Extensions and Uno Extensions libraries in general

## Expected Outcome

You will produce a comprehensive set of Agent Skills that cover all aspects of using MVUX/Reactive Extensions in Uno Platform applications. Each skill will be well-documented, following the specifications provided in the Agent Skills documentation, and will include examples and best practices where applicable.

You should also generate a full index of all the skills you have created, along with a brief description of each skill for easy reference