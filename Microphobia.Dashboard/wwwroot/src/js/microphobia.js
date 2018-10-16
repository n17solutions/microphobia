import clockImage from '../images/clock.gif';
import clockSvg from '../images/clock.svg'
import logo from '../images/logo.png';

import * as signalR from '@aspnet/signalr';

const TASK_STATUS_CREATED = 0;
const TASK_STATUS_WAITINGTORUN = 2;
const TASK_STATUS_RUNNING = 3;
const TASK_STATUS_COMPLETED = 5;
const TASK_STATUS_FAULTED = 7;

const vm = new Vue({
    el: '#app',
    data: {
        clockImage,
        clockSvg,
        logo,
        currentTask: {},
        tasks: [],
        runners: [],
        openTaskModal: false
    },
    components: {
        'TaskItem': {
            props: {
                task: Object,
                icon: Object
            },
            template: '#task-template',
            computed: {
                fallback: function () {
                    return `this.src = '${this.icon.fallback}'`;
                }
            },
            methods: {
                showTaskModal: function (taskId) {
                    this.$root.showTaskModal(taskId);
                }
            }
        },
        'modal': {
            props: {
                currentTask: Object
            },
            template: '#modal-template',
            methods: {
                reenqueueTask: function (taskId) {
                    axios.patch(`./api/tasks/${taskId}`)
                        .then(response => {
                            this.$root.hideTaskModal();
                        });
                }
            }
        }
    },
    mounted() {
        const taskFetcher = () => {
            axios.get('./api/tasks')
                .then(response => {
                    this.tasks = response.data;
                });
        };
        
        const runnerFetcher = () => {
            axios.get('./api/runners')
                .then(response => {
                    this.runners = response.data; 
                });
        };
        
        taskFetcher();
        runnerFetcher();
        
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/microphobia/microphobiahub')
            .configureLogging(signalR.LogLevel.Information)
            .build();
        
        connection.on('RefreshTasks', () => {
            taskFetcher();
        });
        
        connection.start().catch(err => console.error(err.toString()));
    },
    computed: {
        createdTasks: function () {
            return this.tasks.filter(function (task) {
                return task.statusId === TASK_STATUS_CREATED;
            });
        },
        waitingToRunTasks: function () {
            return this.tasks.filter(function (task) {
                return task.statusId === TASK_STATUS_WAITINGTORUN; 
            });
        },
        runningTasks: function () {
            return this.tasks.filter(function (task) {
                return task.statusId === TASK_STATUS_RUNNING;
            });
        },
        completedTasks: function () {
            return this.tasks.filter(function (task) {
                return task.statusId === TASK_STATUS_COMPLETED;
            });
        },
        faultedTasks: function () {
            return this.tasks.filter(function (task) {
                return task.statusId === TASK_STATUS_FAULTED;
            });
        }
    },
    methods: {
        showTaskModal: function (taskId) {
            axios.get(`./api/tasks/${taskId}`)
                .then(response => {
                    this.currentTask = response.data;
                    if (this.currentTask.statusId === TASK_STATUS_FAULTED)
                        this.currentTask.canReEnqueue = true;
                    
                    this.openTaskModal = true;
                });
        },
        hideTaskModal: function () {
            this.currentTask = {};
            this.openTaskModal = false;
        }
    }
});