from nn import NNTraining

training = NNTraining(inputFile='sino-ml-buttons.json', outputFile='sino-ml-buttons.txt')
training.run(numHidden1=40, numEpochs=20)
